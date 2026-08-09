using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CineVision.Model.Exceptions;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CineVision.Model.Messages;
using CineVision.Services;
using CineVision.Model.Enums;

namespace CineVision.Services
{
    public class ProjectionService : BaseCRUDService<Projection, ProjectionResponse, ProjectionSearchObject, ProjectionInsertRequest, ProjectionUpdateRequest>, IProjectionService
    {
        private readonly IAnalyticsNotifier _analyticsNotifier;
        private readonly IEmailService _emailService;
        private readonly string? _stripeSecretKey;
        private readonly ILogger<ProjectionService> _logger;
        private readonly INotificationService _notificationService;

        public ProjectionService(
            CineVisionDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ProjectionInsertRequest> insertValidator,
            IValidator<ProjectionUpdateRequest> updateValidator,
            IAnalyticsNotifier analyticsNotifier,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<ProjectionService> logger,
            INotificationService notificationService)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _analyticsNotifier = analyticsNotifier;
            _emailService = emailService;
            _stripeSecretKey = configuration["Stripe:SecretKey"];
            _logger = logger;
            _notificationService = notificationService;
        }

        protected override IQueryable<Projection> ApplyFilters(IQueryable<Projection> query, ProjectionSearchObject? search)
        {
            // Filtering is handled in GetAllAsync against the database query.
            return query;
        }

        public override async Task<PageResult<ProjectionResponse>> GetAllAsync(ProjectionSearchObject? search = null)
        {
            search ??= new ProjectionSearchObject();
            PagingLimits.Normalize(search);

            var includeSeatStats = search.IncludeSeatStats == true;
            var includePoster = search.IncludePoster == true;

            IQueryable<Projection> query = _dbContext.Projections
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

            return new PageResult<ProjectionResponse>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public override async Task<ProjectionResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Projections
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
                ?? throw new KeyNotFoundException($"Projection with id {id} not found.");

            return MapToResponse(entity, includeMovie: true, includeHall: true, includeSeatStats: true, includePoster: true);
        }

        public override async Task<ProjectionResponse> InsertAsync(ProjectionInsertRequest request)
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

            var entity = new Projection
            {
                MovieId = request.MovieId,
                HallId = request.HallId,
                StartTime = request.StartTime,
                EndTime = endTime,
                BasePrice = request.BasePrice,
                LanguageId = request.LanguageId,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Projections.Add(entity);
            await _dbContext.SaveChangesAsync();

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();

            return await GetByIdAsync(entity.Id);
        }

        public override async Task<ProjectionResponse> UpdateAsync(int id, ProjectionUpdateRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var entity = await _dbContext.Projections.FindAsync(id)
                ?? throw new KeyNotFoundException($"Projection with id {id} not found.");

            var movie = await _dbContext.Movies.FindAsync(request.MovieId)
                ?? throw new ClientException($"Movie {request.MovieId} was not found.");

            await EnsureHallCanBeScheduledAsync(request.HallId);
            await EnsureLanguageExistsAsync(request.LanguageId);

            var endTime = request.StartTime.AddMinutes(movie.DurationMinutes);
            await EnsureNoHallOverlapAsync(request.HallId, request.StartTime, endTime, excludeProjectionId: id);

            entity.MovieId = request.MovieId;
            entity.HallId = request.HallId;
            entity.StartTime = request.StartTime;
            entity.EndTime = endTime;
            entity.BasePrice = request.BasePrice;
            entity.LanguageId = request.LanguageId;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();

            return await GetByIdAsync(entity.Id);
        }

        public async Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id)
        {
            var projection = await _dbContext.Projections
                .AsNoTracking()
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Projection with id {id} not found.");

            var graph = await BookingGraphCascade.CountForProjectionIdsAsync(_dbContext, new[] { id });
            var display = projection.Movie?.Title ?? $"Projection #{id}";

            return BookingGraphCascade.BuildImpact(
                projection.Id,
                display,
                ("Reservations", graph.ReservationCount),
                ("Reserved seats", graph.ReservationSeatCount));
        }

        public override async Task DeleteAsync(int id)
        {
            // Hard cascade: refund paid bookings, notify customers, then delete children then projection.
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            List<Reservation> toNotify;
            string movieTitle;
            string hallName;
            DateTime startTime;
            try
            {
                var projection = await _dbContext.Projections
                    .FirstOrDefaultAsync(s => s.Id == id)
                    ?? throw new KeyNotFoundException($"Projection with id {id} not found.");

                var reservations = await _dbContext.Reservations
                    .Where(r => r.ProjectionId == id)
                    .Include(r => r.User)
                    .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                    .ToListAsync();

                movieTitle = await _dbContext.Movies
                    .Where(m => m.Id == projection.MovieId)
                    .Select(m => m.Title)
                    .FirstOrDefaultAsync() ?? string.Empty;

                hallName = await _dbContext.Halls
                    .Where(h => h.Id == projection.HallId)
                    .Select(h => h.Name)
                    .FirstOrDefaultAsync() ?? string.Empty;

                startTime = projection.StartTime;
                toNotify = reservations
                    .Where(r => r.Status != ReservationStatus.Cancelled)
                    .ToList();

                await BookingGraphCascade.RemoveProjectionsAsync(
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
                        "Projection cancelled",
                        $"Your booking {reservation.ReservationNumber} was cancelled because the projection was removed by staff.",
                        NotificationType.Cancellation);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify user {UserId} about projection cancellation.", reservation.UserId);
                }
            }

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();
        }

        /// <summary>Hall status must allow projections.</summary>
        private async Task EnsureHallCanBeScheduledAsync(int hallId)
        {
            var hall = await _dbContext.Halls
                .Include(h => h.Status)
                .FirstOrDefaultAsync(h => h.Id == hallId)
                ?? throw new ClientException($"Hall {hallId} was not found.");

            if (hall.Status?.AllowsProjections != true)
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

        /// <summary>No other active projection in the same hall may overlap [start, end).</summary>
        private async Task EnsureNoHallOverlapAsync(
            int hallId,
            DateTime start,
            DateTime end,
            int? excludeProjectionId = null)
        {
            if (end <= start)
            {
                throw new ClientException("Projection end time must be after start time.");
            }

            var query = _dbContext.Projections.AsNoTracking()
                .Where(s =>
                    s.HallId == hallId &&
                    s.IsActive &&
                    s.StartTime < end &&
                    s.EndTime > start);

            if (excludeProjectionId.HasValue)
            {
                query = query.Where(s => s.Id != excludeProjectionId.Value);
            }

            var conflict = await query
                .Select(s => new { s.Id, s.StartTime, s.EndTime })
                .FirstOrDefaultAsync();

            if (conflict != null)
            {
                throw new ClientException(
                    $"Hall already has projection #{conflict.Id} from {conflict.StartTime:u} to {conflict.EndTime:u} (UTC). Choose another time or hall.");
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
                    $"Start: {CinemaDateTime.FormatLocal(startTime)}\n\n" +
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

        public async Task<List<ProjectionSeatResponse>> GetSeatsAsync(int projectionId)
        {
            var projection = await _dbContext.Projections
                .AsNoTracking()
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .FirstOrDefaultAsync(s => s.Id == projectionId)
                ?? throw new KeyNotFoundException($"Projection with id {projectionId} not found.");

            var takenSeatIds = await _dbContext.ReservationSeats
                .Where(rs => rs.ProjectionId == projectionId)
                .Select(rs => rs.SeatId)
                .ToListAsync();

            var taken = new HashSet<int>(takenSeatIds);
            var seatsById = projection.Hall.Seats.ToDictionary(s => s.Id);

            foreach (var takenId in takenSeatIds.ToList())
            {
                if (seatsById.TryGetValue(takenId, out var takenSeat) && takenSeat.PartnerSeatId.HasValue)
                {
                    taken.Add(takenSeat.PartnerSeatId.Value);
                }

                foreach (var seat in projection.Hall.Seats)
                {
                    if (seat.PartnerSeatId == takenId)
                    {
                        taken.Add(seat.Id);
                    }
                }
            }

            return projection.Hall.Seats
                .Where(s => s.IsActive)
                .OrderBy(s => s.RowLabel)
                .ThenBy(s => s.SeatNumber)
                .Select(s =>
                {
                    var spots = s.SeatType == SeatType.Couple ? 2 : 1;
                    return new ProjectionSeatResponse
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
                        Price = projection.BasePrice * spots
                    };
                })
                .ToList();
        }

        private ProjectionResponse MapToResponse(
            Projection s,
            bool includeMovie,
            bool includeHall,
            bool includeSeatStats,
            bool includePoster = false)
        {
            var response = _mapper.Map<ProjectionResponse>(s);
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
