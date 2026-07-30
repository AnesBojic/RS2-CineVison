using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Model;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    /// <summary>
    /// Produces aggregated sales / occupancy analytics for the desktop dashboard and reports.
    /// Revenue is counted only from reservations that have actually been paid; tickets sold
    /// count every reserved seat still on record (cancelled reservations release their seats).
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ECommerceDbContext _dbContext;

        public AnalyticsService(ECommerceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardResponse> GetDashboardAsync()
        {
            var capByHall = await GetHallCapacitiesAsync();
            var screenings = await GetScreeningsAsync();
            var seatSales = await GetSeatSalesAsync();
            var avgRatings = await GetAvgRatingsAsync();

            var now = DateTime.UtcNow;

            var response = new DashboardResponse
            {
                TotalRevenue = seatSales.Where(s => s.Status == ReservationStatus.Paid).Sum(s => s.Price),
                TotalTicketsSold = seatSales.Count,
                TotalReservations = await _dbContext.Reservations.CountAsync(r => r.Status != ReservationStatus.Cancelled),
                TotalCustomers = await _dbContext.Users.CountAsync(u =>
                    u.IsActive &&
                    u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == RoleNames.Customer)),
                TotalMovies = await _dbContext.Movies.CountAsync(),
                ActiveMovies = await _dbContext.Movies.CountAsync(m => m.IsActive),
                TotalScreenings = screenings.Count,
                UpcomingScreenings = screenings.Count(s => s.IsActive && s.StartTime > now),
                AverageOccupancyPercent = ComputeAverageOccupancy(screenings, seatSales, capByHall),
                TopMovies = BuildMoviePerformance(screenings, seatSales, capByHall, avgRatings).Take(5).ToList()
            };

            return response;
        }

        public async Task<List<MoviePerformanceResponse>> GetMoviePerformanceAsync(ReportSearchObject? search)
        {
            var capByHall = await GetHallCapacitiesAsync();
            var screenings = FilterScreenings(await GetScreeningsAsync(), search);
            var screeningIds = screenings.Select(s => s.Id).ToHashSet();
            var seatSales = (await GetSeatSalesAsync()).Where(s => screeningIds.Contains(s.ScreeningId));
            var avgRatings = await GetAvgRatingsAsync();

            return BuildMoviePerformance(screenings, seatSales, capByHall, avgRatings);
        }

        public async Task<List<RevenueByPeriodResponse>> GetRevenueByPeriodAsync(ReportSearchObject? search)
        {
            var seatSales = (await GetSeatSalesAsync())
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
            var capByHall = await GetHallCapacitiesAsync();
            var halls = await _dbContext.Halls.Select(h => new { h.Id, h.Name }).ToListAsync();
            var screenings = FilterScreenings(await GetScreeningsAsync(), search);
            var screeningIds = screenings.Select(s => s.Id).ToHashSet();
            var seatSales = (await GetSeatSalesAsync()).Where(s => screeningIds.Contains(s.ScreeningId)).ToList();

            var screeningsByHall = screenings.GroupBy(s => s.HallId).ToDictionary(g => g.Key, g => g.Count());
            var soldByHall = seatSales.GroupBy(s => s.HallId).ToDictionary(g => g.Key, g => g.Count());
            int totalScreenings = screenings.Count;

            var result = new List<HallUtilizationResponse>();
            foreach (var hall in halls)
            {
                capByHall.TryGetValue(hall.Id, out var capacity);
                screeningsByHall.TryGetValue(hall.Id, out var screeningsCount);
                soldByHall.TryGetValue(hall.Id, out var sold);
                int offered = capacity * screeningsCount;

                result.Add(new HallUtilizationResponse
                {
                    HallId = hall.Id,
                    HallName = hall.Name,
                    Capacity = capacity,
                    ScreeningsCount = screeningsCount,
                    ShowCount = screeningsCount,
                    SharePercent = totalScreenings > 0 ? Math.Round((double)screeningsCount / totalScreenings * 100, 1) : 0,
                    SeatsOffered = offered,
                    SeatsSold = sold,
                    UtilizationPercent = offered > 0 ? Math.Round((double)sold / offered * 100, 1) : 0
                });
            }

            return result.OrderByDescending(r => r.UtilizationPercent).ToList();
        }

        public async Task<List<TimeSlotPerformanceResponse>> GetPerformanceByTimeSlotAsync(ReportSearchObject? search)
        {
            var capByHall = await GetHallCapacitiesAsync();
            var screenings = FilterScreenings(await GetScreeningsAsync(), search);
            var screeningIds = screenings.Select(s => s.Id).ToHashSet();
            var seatSales = (await GetSeatSalesAsync()).Where(s => screeningIds.Contains(s.ScreeningId)).ToList();

            var result = new List<TimeSlotPerformanceResponse>();
            foreach (var slot in TimeSlots)
            {
                var slotScreenings = screenings
                    .Where(s => s.StartTime.Hour >= slot.StartHour && s.StartTime.Hour < slot.EndHour)
                    .ToList();
                var slotSales = seatSales
                    .Where(s => s.ScreeningStart.Hour >= slot.StartHour && s.ScreeningStart.Hour < slot.EndHour)
                    .ToList();

                int offered = slotScreenings.Sum(s => capByHall.TryGetValue(s.HallId, out var c) ? c : 0);
                int sold = slotSales.Count;

                result.Add(new TimeSlotPerformanceResponse
                {
                    TimeSlot = slot.Label,
                    TicketsSold = sold,
                    OccupancyPercent = offered > 0 ? Math.Round((double)sold / offered * 100, 1) : 0,
                    Revenue = slotSales.Where(x => x.Status == ReservationStatus.Paid).Sum(x => x.Price)
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

        // Fixed daily time slots shown on the analytics screen.
        // (based on the local hour component of a screening's StartTime).
        private static readonly (string Label, int StartHour, int EndHour)[] TimeSlots =
        {
            ("10:00 AM - 12:00 PM", 10, 12),
            ("12:00 PM - 3:00 PM", 12, 15),
            ("3:00 PM - 6:00 PM", 15, 18),
            ("6:00 PM - 9:00 PM", 18, 21),
            ("9:00 PM - 12:00 AM", 21, 24)
        };

        // ---- helpers -------------------------------------------------------

        private async Task<Dictionary<int, int>> GetHallCapacitiesAsync()
        {
            var rows = await _dbContext.Seats
                .Where(s => s.IsActive)
                .GroupBy(s => s.HallId)
                .Select(g => new { HallId = g.Key, Count = g.Count() })
                .ToListAsync();

            return rows.ToDictionary(x => x.HallId, x => x.Count);
        }

        private async Task<Dictionary<int, double>> GetAvgRatingsAsync()
        {
            var rows = await _dbContext.Reviews
                .GroupBy(r => r.MovieId)
                .Select(g => new { MovieId = g.Key, Avg = g.Average(x => (double)x.Rating) })
                .ToListAsync();

            return rows.ToDictionary(x => x.MovieId, x => Math.Round(x.Avg, 1));
        }

        private async Task<List<ScreeningRow>> GetScreeningsAsync()
        {
            return await _dbContext.Screenings
                .Select(s => new ScreeningRow
                {
                    Id = s.Id,
                    MovieId = s.MovieId,
                    MovieTitle = s.Movie.Title,
                    MoviePosterBase64 = s.Movie.PosterImageBase64,
                    HallId = s.HallId,
                    StartTime = s.StartTime,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        private async Task<List<SeatSale>> GetSeatSalesAsync()
        {
            return await _dbContext.ReservationSeats
                .Select(rs => new SeatSale
                {
                    ReservationId = rs.ReservationId,
                    ScreeningId = rs.ScreeningId,
                    MovieId = rs.Screening.MovieId,
                    MovieTitle = rs.Screening.Movie.Title,
                    HallId = rs.Screening.HallId,
                    Price = rs.Price,
                    Status = rs.Reservation.Status,
                    ReservationDate = rs.Reservation.ReservationDate,
                    ScreeningStart = rs.Screening.StartTime
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
                    PosterImageBase64 = group.First().MoviePosterBase64,
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
            return (!from.HasValue || value >= from.Value) && (!to.HasValue || value <= to.Value);
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            int diff = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
            return date.Date.AddDays(-diff);
        }

        private class ScreeningRow
        {
            public int Id { get; set; }
            public int MovieId { get; set; }
            public string MovieTitle { get; set; } = string.Empty;
            public string? MoviePosterBase64 { get; set; }
            public int HallId { get; set; }
            public DateTime StartTime { get; set; }
            public bool IsActive { get; set; }
        }

        private class SeatSale
        {
            public int ReservationId { get; set; }
            public int ScreeningId { get; set; }
            public int MovieId { get; set; }
            public string MovieTitle { get; set; } = string.Empty;
            public int HallId { get; set; }
            public decimal Price { get; set; }
            public ReservationStatus Status { get; set; }
            public DateTime ReservationDate { get; set; }
            public DateTime ScreeningStart { get; set; }
        }
    }
}
