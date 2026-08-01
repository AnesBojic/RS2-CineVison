using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using eCommerce.Model.Messages;
using eCommerce.Services;

namespace eCommerce.Services
{
    public class ScreeningService : BaseCRUDService<Screening, ScreeningResponse, ScreeningSearchObject, ScreeningInsertRequest, ScreeningUpdateRequest>, IScreeningService
    {
        private readonly IAnalyticsNotifier _analyticsNotifier;
        private readonly IEmailService _emailService;
        private readonly string? _stripeSecretKey;
        private readonly ILogger<ScreeningService> _logger;
        private readonly INotificationService _notificationService;

        public ScreeningService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ScreeningInsertRequest> insertValidator,
            IValidator<ScreeningUpdateRequest> updateValidator,
            IAnalyticsNotifier analyticsNotifier,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<ScreeningService> logger,
            INotificationService notificationService)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _analyticsNotifier = analyticsNotifier;
            _emailService = emailService;
            _stripeSecretKey = configuration["Stripe:SecretKey"];
            _logger = logger;
            _notificationService = notificationService;
        }

        protected override IEnumerable<Screening> ApplyFilters(IEnumerable<Screening> query, ScreeningSearchObject? search)
        {
            // Filtering is handled in GetAllAsync against the database query.
            return query;
        }

        public override async Task<PageResult<ScreeningResponse>> GetAllAsync(ScreeningSearchObject? search = null)
        {
            search ??= new ScreeningSearchObject();
            PagingLimits.Normalize(search);

            var includeSeatStats = search.IncludeSeatStats == true;
            var includePoster = search.IncludePoster == true;

            IQueryable<Screening> query = _dbContext.Screenings
                .AsNoTracking()
                .Include(s => s.Language)
                .Include(s => s.Movie).ThenInclude(m => m.Language)
                .Include(s => s.Movie).ThenInclude(m => m.AgeRating);

            if (includeSeatStats)
            {
                query = query
                    .Include(s => s.Hall).ThenInclude(h => h.Seats)
                    .Include(s => s.Hall).ThenInclude(h => h.ScreenType)
                    .Include(s => s.Hall).ThenInclude(h => h.Status)
                    .Include(s => s.ReservationSeats);
            }
            else
            {
                query = query
                    .Include(s => s.Hall).ThenInclude(h => h.ScreenType)
                    .Include(s => s.Hall).ThenInclude(h => h.Status);
            }

            if (search.MovieId.HasValue)
            {
                query = query.Where(s => s.MovieId == search.MovieId.Value);
            }
            if (search.HallId.HasValue)
            {
                query = query.Where(s => s.HallId == search.HallId.Value);
            }
            if (search.FromDate.HasValue)
            {
                query = query.Where(s => s.StartTime >= search.FromDate.Value);
            }
            if (search.ToDate.HasValue)
            {
                query = query.Where(s => s.StartTime <= search.ToDate.Value);
            }
            if (search.OnlyUpcoming == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(s => s.StartTime >= now);
            }

            // Soft-deleted projections stay in DB but are hidden from normal lists.
            if (search.IncludeInactive != true)
            {
                query = query.Where(s => s.IsActive);
            }

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = await query.CountAsync();
            }

            query = query.OrderByDescending(s => s.StartTime)
                .Skip((search.Page!.Value - 1) * search.PageSize!.Value)
                .Take(search.PageSize.Value);

            var entities = await query.ToListAsync();
            var items = entities.Select(s => MapToResponse(
                s,
                search.IncludeMovie == true,
                search.IncludeHall == true,
                includeSeatStats,
                includePoster)).ToList();

            return new PageResult<ScreeningResponse>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public override async Task<ScreeningResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Screenings
                .AsNoTracking()
                .Include(s => s.Language)
                .Include(s => s.Movie).ThenInclude(m => m.Genre)
                .Include(s => s.Movie).ThenInclude(m => m.Language)
                .Include(s => s.Movie).ThenInclude(m => m.AgeRating)
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .Include(s => s.Hall).ThenInclude(h => h.ScreenType)
                .Include(s => s.Hall).ThenInclude(h => h.Status)
                .Include(s => s.ReservationSeats)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Screening with id {id} not found.");

            return MapToResponse(entity, includeMovie: true, includeHall: true, includeSeatStats: true, includePoster: true);
        }

        public override async Task<ScreeningResponse> InsertAsync(ScreeningInsertRequest request)
        {
            var validationResult = await _insertValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var movie = await _dbContext.Movies.FindAsync(request.MovieId)
                ?? throw new ClientException($"Movie {request.MovieId} was not found.");

            await EnsureHallCanBeScheduledAsync(request.HallId);
            await EnsureLanguageExistsAsync(request.LanguageId);

            var endTime = request.StartTime.AddMinutes(movie.DurationMinutes);
            await EnsureNoHallOverlapAsync(request.HallId, request.StartTime, endTime);

            var entity = new Screening
            {
                MovieId = request.MovieId,
                HallId = request.HallId,
                StartTime = request.StartTime,
                EndTime = endTime,
                BasePrice = request.BasePrice,
                LanguageId = request.LanguageId,
                HasSubtitles = request.HasSubtitles,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Screenings.Add(entity);
            await _dbContext.SaveChangesAsync();

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();

            return await GetByIdAsync(entity.Id);
        }

        public override async Task<ScreeningResponse> UpdateAsync(int id, ScreeningUpdateRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var entity = await _dbContext.Screenings.FindAsync(id)
                ?? throw new KeyNotFoundException($"Screening with id {id} not found.");

            var movie = await _dbContext.Movies.FindAsync(request.MovieId)
                ?? throw new ClientException($"Movie {request.MovieId} was not found.");

            await EnsureHallCanBeScheduledAsync(request.HallId);
            await EnsureLanguageExistsAsync(request.LanguageId);

            var endTime = request.StartTime.AddMinutes(movie.DurationMinutes);
            await EnsureNoHallOverlapAsync(request.HallId, request.StartTime, endTime, excludeScreeningId: id);

            entity.MovieId = request.MovieId;
            entity.HallId = request.HallId;
            entity.StartTime = request.StartTime;
            entity.EndTime = endTime;
            entity.BasePrice = request.BasePrice;
            entity.LanguageId = request.LanguageId;
            entity.HasSubtitles = request.HasSubtitles;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();

            return await GetByIdAsync(entity.Id);
        }

        public async Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id)
        {
            var screening = await _dbContext.Screenings
                .AsNoTracking()
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Screening with id {id} not found.");

            var graph = await BookingGraphCascade.CountForScreeningIdsAsync(_dbContext, new[] { id });
            var display = screening.Movie?.Title ?? $"Projection #{id}";

            return BookingGraphCascade.BuildImpact(
                screening.Id,
                display,
                ("Reservations", graph.ReservationCount),
                ("Reserved seats", graph.ReservationSeatCount));
        }

        public override async Task DeleteAsync(int id)
        {
            // Hard cascade: refund paid bookings, notify customers, then delete children then screening.
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            List<Reservation> toNotify;
            string movieTitle;
            string hallName;
            DateTime startTime;
            try
            {
                var screening = await _dbContext.Screenings
                    .FirstOrDefaultAsync(s => s.Id == id)
                    ?? throw new KeyNotFoundException($"Screening with id {id} not found.");

                var reservations = await _dbContext.Reservations
                    .Where(r => r.ScreeningId == id)
                    .Include(r => r.User)
                    .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                    .ToListAsync();

                movieTitle = await _dbContext.Movies
                    .Where(m => m.Id == screening.MovieId)
                    .Select(m => m.Title)
                    .FirstOrDefaultAsync() ?? string.Empty;

                hallName = await _dbContext.Halls
                    .Where(h => h.Id == screening.HallId)
                    .Select(h => h.Name)
                    .FirstOrDefaultAsync() ?? string.Empty;

                startTime = screening.StartTime;
                toNotify = reservations
                    .Where(r => r.Status != ReservationStatus.Cancelled)
                    .ToList();

                await BookingGraphCascade.RemoveScreeningsAsync(
                    _dbContext,
                    new[] { id },
                    paymentIntentId => StripeRefundHelper.TryRefundAsync(_stripeSecretKey, paymentIntentId, _logger));

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            await QueueCancellationEmailsAsync(toNotify, movieTitle, hallName, startTime);

            foreach (var reservation in toNotify)
            {
                try
                {
                    await _notificationService.CreateAsync(
                        reservation.UserId,
                        "Screening cancelled",
                        $"Your booking {reservation.ReservationNumber} was cancelled because the projection was removed by staff.",
                        "Cancellation");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify user {UserId} about screening cancellation.", reservation.UserId);
                }
            }

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();
        }

        /// <summary>
        /// Ensures no other active screening in the same hall overlaps [start, end).
        /// </summary>
        /// <summary>
        /// A projection can only be scheduled in a hall whose status allows it. The rule lives on
        /// the HallStatuses row, so staff can add statuses without touching this check.
        /// </summary>
        private async Task EnsureHallCanBeScheduledAsync(int hallId)
        {
            var hall = await _dbContext.Halls
                .Include(h => h.Status)
                .FirstOrDefaultAsync(h => h.Id == hallId)
                ?? throw new ClientException($"Hall {hallId} was not found.");

            if (hall.Status?.AllowsScreenings != true)
            {
                var statusName = hall.Status?.Name ?? "unknown";
                throw new ClientException(
                    $"Hall '{hall.Name}' is not available (status: {statusName}). Projections can only be scheduled in halls whose status allows it.");
            }
        }

        private async Task EnsureLanguageExistsAsync(int? languageId)
        {
            if (languageId == null)
            {
                return;
            }

            var exists = await _dbContext.Languages.AnyAsync(l => l.Id == languageId.Value);
            if (!exists)
            {
                throw new ClientException("The selected language no longer exists. Refresh and pick another one.");
            }
        }

        private async Task EnsureNoHallOverlapAsync(
            int hallId,
            DateTime start,
            DateTime end,
            int? excludeScreeningId = null)
        {
            if (end <= start)
            {
                throw new ClientException("Screening end time must be after start time.");
            }

            var query = _dbContext.Screenings.AsNoTracking()
                .Where(s =>
                    s.HallId == hallId &&
                    s.IsActive &&
                    s.StartTime < end &&
                    s.EndTime > start);

            if (excludeScreeningId.HasValue)
            {
                query = query.Where(s => s.Id != excludeScreeningId.Value);
            }

            var conflict = await query
                .Select(s => new { s.Id, s.StartTime, s.EndTime })
                .FirstOrDefaultAsync();

            if (conflict != null)
            {
                throw new ClientException(
                    $"Hall already has screening #{conflict.Id} from {conflict.StartTime:u} to {conflict.EndTime:u} (UTC). Choose another time or hall.");
            }
        }

        private async Task QueueCancellationEmailsAsync(
            List<Reservation> reservations,
            string movieTitle,
            string hallName,
            DateTime startTime)
        {
            // One email per customer (unique email address).
            var byEmail = reservations
                .Select(r =>
                {
                    var email = string.IsNullOrWhiteSpace(r.CustomerEmail) ? r.User?.Email : r.CustomerEmail;
                    return new { Reservation = r, Email = email };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                .GroupBy(x => x.Email!);

            foreach (var group in byEmail)
            {
                var reservationsForUser = group.Select(x => x.Reservation).ToList();
                var first = reservationsForUser.FirstOrDefault();
                var userFirstName = first?.User?.FirstName ?? string.Empty;

                var seatLines = new List<string>();
                foreach (var r in reservationsForUser)
                {
                    var seats = r.ReservationSeats
                        .Select(rs => rs.Seat != null ? $"{rs.Seat.RowLabel}{rs.Seat.SeatNumber}" : null)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    seatLines.Add(
                        $"- {r.ReservationNumber}: {string.Join(", ", seats)}");
                }

                var subject = $"CineVision: projection cancelled ({movieTitle})";
                var body =
                    $"Hi {userFirstName},\n\n" +
                    $"Your booking(s) for the following projection were cancelled because the projection was deleted by admin/staff.\n\n" +
                    $"Movie: {movieTitle}\n" +
                    $"Hall: {hallName}\n" +
                    $"Start: {startTime:yyyy-MM-dd HH:mm} UTC\n\n" +
                    $"Reservations:\n{string.Join("\n", seatLines)}\n\n" +
                    $"If you have already paid, a refund will be attempted automatically.\n\n" +
                    $"Thank you,\nCineVision";

                try
                {
                    await _emailService.QueueEmailAsync(new EmailMessage
                    {
                        To = group.Key,
                        Subject = subject,
                        Body = body,
                        IsHtml = false
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to queue cancellation email to {Email}.", group.Key);
                }
            }
        }

        public async Task<List<ScreeningSeatResponse>> GetSeatsAsync(int screeningId)
        {
            var screening = await _dbContext.Screenings
                .AsNoTracking()
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .FirstOrDefaultAsync(s => s.Id == screeningId)
                ?? throw new KeyNotFoundException($"Screening with id {screeningId} not found.");

            var takenSeatIds = await _dbContext.ReservationSeats
                .Where(rs => rs.ScreeningId == screeningId)
                .Select(rs => rs.SeatId)
                .ToListAsync();

            var taken = new HashSet<int>(takenSeatIds);
            var seatsById = screening.Hall.Seats.ToDictionary(s => s.Id);

            foreach (var takenId in takenSeatIds.ToList())
            {
                if (seatsById.TryGetValue(takenId, out var takenSeat) && takenSeat.PartnerSeatId.HasValue)
                {
                    taken.Add(takenSeat.PartnerSeatId.Value);
                }

                foreach (var seat in screening.Hall.Seats)
                {
                    if (seat.PartnerSeatId == takenId)
                    {
                        taken.Add(seat.Id);
                    }
                }
            }

            return screening.Hall.Seats
                .Where(s => s.IsActive)
                .OrderBy(s => s.RowLabel)
                .ThenBy(s => s.SeatNumber)
                .Select(s =>
                {
                    var spots = s.SeatType == SeatType.Couple ? 2 : 1;
                    return new ScreeningSeatResponse
                    {
                        SeatId = s.Id,
                        HallId = s.HallId,
                        RowLabel = s.RowLabel,
                        SeatNumber = s.SeatNumber,
                        SeatType = (int)s.SeatType,
                        PartnerSeatId = s.PartnerSeatId,
                        SpotsOccupied = spots,
                        IsTaken = taken.Contains(s.Id) ||
                                  (s.PartnerSeatId.HasValue && taken.Contains(s.PartnerSeatId.Value)),
                        Price = screening.BasePrice * spots
                    };
                })
                .ToList();
        }

        private ScreeningResponse MapToResponse(
            Screening s,
            bool includeMovie,
            bool includeHall,
            bool includeSeatStats,
            bool includePoster = false)
        {
            var response = _mapper.Map<ScreeningResponse>(s);
            response.MovieTitle = s.Movie?.Title ?? string.Empty;
            response.MoviePosterBase64 = includePoster ? s.Movie?.PosterImageBase64 : null;
            response.HallName = s.Hall?.Name ?? string.Empty;

            if (includeSeatStats)
            {
                var totalSeats = s.Hall?.Seats.Count(x => x.IsActive) ?? 0;
                response.TotalSeats = totalSeats;
                response.AvailableSeats = Math.Max(0, totalSeats - (s.ReservationSeats?.Count ?? 0));
            }

            if (includeMovie && s.Movie != null)
            {
                response.Movie = _mapper.Map<MovieResponse>(s.Movie);
            }
            if (includeHall && s.Hall != null)
            {
                response.Hall = _mapper.Map<HallResponse>(s.Hall);
                response.Hall.SeatCount = includeSeatStats
                    ? s.Hall.Seats.Count
                    : 0;
            }

            return response;
        }
    }
}
