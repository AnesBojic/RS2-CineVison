using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Model;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using eCommerce.Model.Enums;

namespace eCommerce.Services
{
    /// <summary>
    /// Produces aggregated sales / occupancy analytics for the desktop dashboard and reports.
    /// Revenue is counted only from reservations that have actually been paid; tickets sold
    /// count every reserved seat still on record (cancelled reservations release their seats).
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private static readonly TimeSpan SnapshotTtl = TimeSpan.FromSeconds(30);
        private const string SnapshotCacheKey = "analytics:snapshot:v2";

        private readonly ECommerceDbContext _dbContext;
        private readonly IMemoryCache _cache;

        public AnalyticsService(ECommerceDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<DashboardResponse> GetDashboardAsync()
        {
            var snapshot = await GetSnapshotAsync();
            var now = DateTime.UtcNow;

            var topMovies = BuildMoviePerformance(snapshot.Screenings, snapshot.SeatSales, snapshot.CapByHall, snapshot.AvgRatings)
                .Take(5)
                .ToList();
            await AttachPostersAsync(topMovies);

            return new DashboardResponse
            {
                TotalRevenue = snapshot.SeatSales.Where(s => s.Status == ReservationStatus.Paid).Sum(s => s.Price),
                TotalTicketsSold = snapshot.SeatSales.Count,
                TotalReservations = snapshot.TotalReservations,
                TotalCustomers = snapshot.TotalCustomers,
                TotalMovies = snapshot.TotalMovies,
                ActiveMovies = snapshot.ActiveMovies,
                TotalScreenings = snapshot.Screenings.Count,
                UpcomingScreenings = snapshot.Screenings.Count(s => s.IsActive && s.StartTime > now),
                AverageOccupancyPercent = ComputeAverageOccupancy(snapshot.Screenings, snapshot.SeatSales, snapshot.CapByHall),
                TopMovies = topMovies
            };
        }

        public async Task<List<MoviePerformanceResponse>> GetMoviePerformanceAsync(ReportSearchObject? search)
        {
            var snapshot = await GetSnapshotAsync();
            var screenings = FilterScreenings(snapshot.Screenings, search);
            var screeningIds = screenings.Select(s => s.Id).ToHashSet();
            var seatSales = snapshot.SeatSales.Where(s => screeningIds.Contains(s.ScreeningId));

            var result = BuildMoviePerformance(screenings, seatSales, snapshot.CapByHall, snapshot.AvgRatings);
            await AttachPostersAsync(result);
            return result;
        }

        public async Task<List<RevenueByPeriodResponse>> GetRevenueByPeriodAsync(ReportSearchObject? search)
        {
            var snapshot = await GetSnapshotAsync();
            var seatSales = snapshot.SeatSales
                .Where(s => InRange(s.ReservationDate, search?.DateFrom, search?.DateTo))
                .ToList();

            var groupBy = (search?.GroupBy ?? "day").Trim().ToLowerInvariant();

            Func<DateTime, DateTime> bucket = groupBy switch
            {
                "month" => d => new DateTime(d.Year, d.Month, 1),
                "week" => StartOfWeek,
                _ => d => d.Date
            };
            Func<DateTime, string> label = groupBy switch
            {
                "month" => d => d.ToString("yyyy-MM"),
                _ => d => d.ToString("yyyy-MM-dd")
            };

            return seatSales
                .GroupBy(s => bucket(s.ReservationDate))
                .OrderBy(g => g.Key)
                .Select(g => new RevenueByPeriodResponse
                {
                    PeriodStart = g.Key,
                    Period = label(g.Key),
                    Revenue = g.Where(x => x.Status == ReservationStatus.Paid).Sum(x => x.Price),
                    TicketsSold = g.Count(),
                    ReservationsCount = g.Select(x => x.ReservationId).Distinct().Count()
                })
                .ToList();
        }

        public async Task<List<HallUtilizationResponse>> GetHallUtilizationAsync(ReportSearchObject? search)
        {
            var snapshot = await GetSnapshotAsync();
            var screenings = FilterScreenings(snapshot.Screenings, search);
            var screeningIds = screenings.Select(s => s.Id).ToHashSet();
            var seatSales = snapshot.SeatSales.Where(s => screeningIds.Contains(s.ScreeningId)).ToList();

            var screeningsByHall = screenings.GroupBy(s => s.HallId).ToDictionary(g => g.Key, g => g.Count());
            var soldByHall = seatSales.GroupBy(s => s.HallId).ToDictionary(g => g.Key, g => g.Count());
            var totalScreenings = Math.Max(1, screenings.Count);

            var halls = await _dbContext.Halls
                .AsNoTracking()
                .Select(h => new { h.Id, h.Name })
                .ToListAsync();

            return halls
                .Select(hall =>
                {
                    snapshot.CapByHall.TryGetValue(hall.Id, out var capacity);
                    screeningsByHall.TryGetValue(hall.Id, out var screeningsCount);
                    soldByHall.TryGetValue(hall.Id, out var sold);
                    var seatsOffered = capacity * screeningsCount;
                    return new HallUtilizationResponse
                    {
                        HallId = hall.Id,
                        HallName = hall.Name,
                        Capacity = capacity,
                        ScreeningsCount = screeningsCount,
                        ShowCount = screeningsCount,
                        SharePercent = Math.Round((double)screeningsCount / totalScreenings * 100, 1),
                        SeatsOffered = seatsOffered,
                        SeatsSold = sold,
                        UtilizationPercent = seatsOffered > 0 ? Math.Round((double)sold / seatsOffered * 100, 1) : 0
                    };
                })
                .OrderByDescending(h => h.UtilizationPercent)
                .ThenByDescending(h => h.SharePercent)
                .ToList();
        }

        public async Task<List<TimeSlotPerformanceResponse>> GetPerformanceByTimeSlotAsync(ReportSearchObject? search)
        {
            var snapshot = await GetSnapshotAsync();
            var screenings = FilterScreenings(snapshot.Screenings, search);
            var soldByScreening = snapshot.SeatSales
                .GroupBy(s => s.ScreeningId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var slots = new (string Label, Func<DateTime, bool> Match)[]
            {
                ("Morning (before 12:00)", t => t.Hour < 12),
                ("Afternoon (12:00–17:00)", t => t.Hour is >= 12 and < 17),
                ("Evening (17:00–21:00)", t => t.Hour is >= 17 and < 21),
                ("Night (after 21:00)", t => t.Hour >= 21)
            };

            var result = new List<TimeSlotPerformanceResponse>();
            foreach (var (label, match) in slots)
            {
                var slotScreenings = screenings.Where(s => match(s.StartTime)).ToList();
                int offered = slotScreenings.Sum(s => snapshot.CapByHall.TryGetValue(s.HallId, out var c) ? c : 0);
                var sales = slotScreenings
                    .SelectMany(s => soldByScreening.TryGetValue(s.Id, out var list) ? list : Enumerable.Empty<SeatSale>())
                    .ToList();

                result.Add(new TimeSlotPerformanceResponse
                {
                    TimeSlot = label,
                    TicketsSold = sales.Count,
                    OccupancyPercent = offered > 0 ? Math.Round((double)sales.Count / offered * 100, 1) : 0,
                    Revenue = sales.Where(x => x.Status == ReservationStatus.Paid).Sum(x => x.Price)
                });
            }

            return result;
        }

        public async Task<AnalyticsLiveSnapshotResponse> GetLiveSnapshotAsync()
        {
            return new AnalyticsLiveSnapshotResponse
            {
                Dashboard = await GetDashboardAsync(),
                MoviePerformance = await GetMoviePerformanceAsync(null),
                TimeSlotPerformance = await GetPerformanceByTimeSlotAsync(null),
                HallUtilization = await GetHallUtilizationAsync(null),
                UpdatedAt = DateTime.UtcNow
            };
        }

        private async Task AttachPostersAsync(List<MoviePerformanceResponse> movies)
        {
            if (movies.Count == 0)
            {
                return;
            }

            var ids = movies.Select(m => m.MovieId).Distinct().ToList();
            var posters = await _dbContext.Movies
                .AsNoTracking()
                .Where(m => ids.Contains(m.Id))
                .Select(m => new { m.Id, m.PosterImageBase64 })
                .ToDictionaryAsync(x => x.Id, x => x.PosterImageBase64);

            foreach (var movie in movies)
            {
                if (posters.TryGetValue(movie.MovieId, out var poster))
                {
                    movie.PosterImageBase64 = poster;
                }
            }
        }

        private async Task<AnalyticsSnapshot> GetSnapshotAsync()
        {
            return await _cache.GetOrCreateAsync(SnapshotCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = SnapshotTtl;
                return await LoadSnapshotAsync();
            }) ?? await LoadSnapshotAsync();
        }

        private async Task<AnalyticsSnapshot> LoadSnapshotAsync()
        {
            var capByHall = await GetHallCapacitiesAsync();
            var screenings = await GetScreeningsAsync();
            var seatSales = await GetSeatSalesAsync();
            var avgRatings = await GetAvgRatingsAsync();

            var totalReservations = await _dbContext.Reservations.CountAsync(r => r.Status != ReservationStatus.Cancelled);
            var totalCustomers = await _dbContext.Users.CountAsync(u =>
                u.IsActive &&
                u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == RoleNames.Customer));
            var totalMovies = await _dbContext.Movies.CountAsync();
            var activeMovies = await _dbContext.Movies.CountAsync(m => m.IsActive);

            return new AnalyticsSnapshot(
                capByHall,
                screenings,
                seatSales,
                avgRatings,
                totalReservations,
                totalCustomers,
                totalMovies,
                activeMovies);
        }

        private async Task<Dictionary<int, int>> GetHallCapacitiesAsync()
        {
            var rows = await _dbContext.Seats
                .AsNoTracking()
                .Where(s => s.IsActive)
                .GroupBy(s => s.HallId)
                .Select(g => new { HallId = g.Key, Count = g.Count() })
                .ToListAsync();

            return rows.ToDictionary(x => x.HallId, x => x.Count);
        }

        private async Task<Dictionary<int, double>> GetAvgRatingsAsync()
        {
            var rows = await _dbContext.Reviews
                .AsNoTracking()
                .GroupBy(r => r.MovieId)
                .Select(g => new { MovieId = g.Key, Avg = g.Average(x => (double)x.Rating) })
                .ToListAsync();

            return rows.ToDictionary(x => x.MovieId, x => Math.Round(x.Avg, 1));
        }

        private async Task<List<ScreeningRow>> GetScreeningsAsync()
        {
            return await _dbContext.Screenings
                .AsNoTracking()
                .Select(s => new ScreeningRow
                {
                    Id = s.Id,
                    MovieId = s.MovieId,
                    MovieTitle = s.Movie.Title,
                    HallId = s.HallId,
                    StartTime = s.StartTime,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        private async Task<List<SeatSale>> GetSeatSalesAsync()
        {
            return await _dbContext.ReservationSeats
                .AsNoTracking()
                .Select(rs => new SeatSale
                {
                    ReservationId = rs.ReservationId,
                    ScreeningId = rs.ScreeningId,
                    MovieId = rs.Screening.MovieId,
                    HallId = rs.Screening.HallId,
                    Price = rs.Price,
                    Status = rs.Reservation.Status,
                    ReservationDate = rs.Reservation.ReservationDate
                })
                .ToListAsync();
        }

        private static List<ScreeningRow> FilterScreenings(List<ScreeningRow> screenings, ReportSearchObject? search)
        {
            return screenings
                .Where(s => InRange(s.StartTime, search?.DateFrom, search?.DateTo))
                .ToList();
        }

        private static List<MoviePerformanceResponse> BuildMoviePerformance(
            IEnumerable<ScreeningRow> screenings,
            IEnumerable<SeatSale> seatSales,
            IReadOnlyDictionary<int, int> capByHall,
            IReadOnlyDictionary<int, double> avgRatingByMovie)
        {
            var salesByMovie = seatSales.GroupBy(s => s.MovieId).ToDictionary(g => g.Key, g => g.ToList());
            var result = new List<MoviePerformanceResponse>();

            foreach (var group in screenings.GroupBy(s => s.MovieId))
            {
                int movieId = group.Key;
                int offered = group.Sum(s => capByHall.TryGetValue(s.HallId, out var c) ? c : 0);

                int tickets = 0;
                decimal revenue = 0m;
                int reservations = 0;
                if (salesByMovie.TryGetValue(movieId, out var sales))
                {
                    tickets = sales.Count;
                    revenue = sales.Where(x => x.Status == ReservationStatus.Paid).Sum(x => x.Price);
                    reservations = sales.Select(x => x.ReservationId).Distinct().Count();
                }

                result.Add(new MoviePerformanceResponse
                {
                    MovieId = movieId,
                    Title = group.First().MovieTitle,
                    ScreeningsCount = group.Count(),
                    ReservationsCount = reservations,
                    TicketsSold = tickets,
                    Revenue = revenue,
                    OccupancyPercent = offered > 0 ? Math.Round((double)tickets / offered * 100, 1) : 0,
                    AvgRating = avgRatingByMovie.TryGetValue(movieId, out var avg) ? avg : (double?)null
                });
            }

            return result
                .OrderByDescending(r => r.Revenue)
                .ThenByDescending(r => r.TicketsSold)
                .ToList();
        }

        private static double ComputeAverageOccupancy(
            IEnumerable<ScreeningRow> screenings,
            IEnumerable<SeatSale> seatSales,
            IReadOnlyDictionary<int, int> capByHall)
        {
            var soldByScreening = seatSales.GroupBy(s => s.ScreeningId).ToDictionary(g => g.Key, g => g.Count());

            var occupancies = new List<double>();
            foreach (var s in screenings)
            {
                if (!capByHall.TryGetValue(s.HallId, out var capacity) || capacity <= 0)
                {
                    continue;
                }
                soldByScreening.TryGetValue(s.Id, out var sold);
                occupancies.Add((double)sold / capacity * 100);
            }

            return occupancies.Count > 0 ? Math.Round(occupancies.Average(), 1) : 0;
        }

        private static bool InRange(DateTime value, DateTime? from, DateTime? to)
        {
            if (from.HasValue && value < from.Value) return false;
            if (to.HasValue && value > to.Value) return false;
            return true;
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
        }

        private sealed record AnalyticsSnapshot(
            Dictionary<int, int> CapByHall,
            List<ScreeningRow> Screenings,
            List<SeatSale> SeatSales,
            Dictionary<int, double> AvgRatings,
            int TotalReservations,
            int TotalCustomers,
            int TotalMovies,
            int ActiveMovies);

        private sealed class ScreeningRow
        {
            public int Id { get; set; }
            public int MovieId { get; set; }
            public string MovieTitle { get; set; } = string.Empty;
            public int HallId { get; set; }
            public DateTime StartTime { get; set; }
            public bool IsActive { get; set; }
        }

        private sealed class SeatSale
        {
            public int ReservationId { get; set; }
            public int ScreeningId { get; set; }
            public int MovieId { get; set; }
            public int HallId { get; set; }
            public decimal Price { get; set; }
            public ReservationStatus Status { get; set; }
            public DateTime ReservationDate { get; set; }
        }
    }
}
