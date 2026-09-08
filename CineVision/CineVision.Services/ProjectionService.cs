using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CineVision.Model;
using CineVision.Model.Exceptions;
using CineVision.Model.Messages;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CineVision.Model.Enums;

namespace CineVision.Services
{
    public class ProjectionService : BaseCRUDService<Projection, ProjectionResponse, ProjectionSearchObject, ProjectionInsertRequest, ProjectionUpdateRequest>, IProjectionService
    {
        private readonly IAnalyticsNotifier _analyticsNotifier;
        private readonly ISeatHoldService _seatHoldService;
        private readonly IReservationService _reservationService;
        private readonly IEmailService _emailService;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly ILogger<ProjectionService> _logger;
        private readonly IValidator<ProjectionCancelRequest> _cancelValidator;

        public ProjectionService(
            CineVisionDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ProjectionInsertRequest> insertValidator,
            IValidator<ProjectionUpdateRequest> updateValidator,
            IValidator<ProjectionCancelRequest> cancelValidator,
            IAnalyticsNotifier analyticsNotifier,
            ISeatHoldService seatHoldService,
            IReservationService reservationService,
            IEmailService emailService,
            IAuthenticatedUserAccessor userAccessor,
            ILogger<ProjectionService> logger)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _seatHoldService = seatHoldService;
            _analyticsNotifier = analyticsNotifier;
            _reservationService = reservationService;
            _emailService = emailService;
            _userAccessor = userAccessor;
            _logger = logger;
            _cancelValidator = cancelValidator;
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
            else if (search.ActiveHallsOnly == true)
            {
                query = query.Where(s => s.Hall.Status != null && s.Hall.Status.AllowsProjections);
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
                query = query.Where(s =>
                    s.CancelledAt == null &&
                    s.StartTime >= now &&
                    s.Hall.Status!.AllowsProjections);
            }

            var status = search.Status?.Trim().ToLowerInvariant();
            if (status == "upcoming")
            {
                var now = DateTime.UtcNow;
                query = query.Where(s => s.CancelledAt == null && s.StartTime >= now);
            }
            else if (status == "live")
            {
                var now = DateTime.UtcNow;
                query = query.Where(s =>
                    s.CancelledAt == null &&
                    s.StartTime.AddMinutes(s.Movie.DurationMinutes) > now);
            }
            else if (status == "past")
            {
                var now = DateTime.UtcNow;
                query = query.Where(s => s.CancelledAt == null && s.StartTime < now);
            }
            else if (status == "cancelled")
            {
                query = query.Where(s => s.CancelledAt != null);
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
            var bookedIds = await ProjectionIdsWithBookingsAsync(entities.Select(s => s.Id).ToList());
            var items = entities.Select(s => MapToResponse(
                s,
                search.IncludeMovie == true,
                search.IncludeHall == true,
                includeSeatStats,
                includePoster,
                hasBookings: bookedIds.Contains(s.Id))).ToList();

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

            return MapToResponse(
                entity,
                includeMovie: true,
                includeHall: true,
                includeSeatStats: true,
                includePoster: true,
                hasBookings: await _dbContext.Reservations.AnyAsync(r => r.ProjectionId == id));
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
            await ScheduleConflictGuard.EnsureNoHallOverlapAsync(_dbContext, request.HallId, request.StartTime, endTime);

            var entity = new Projection
            {
                MovieId = request.MovieId,
                HallId = request.HallId,
                StartTime = request.StartTime,
                BasePrice = request.BasePrice,
                LanguageId = request.LanguageId,
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

            if (entity.CancelledAt != null)
            {
                throw new ClientException("This projection was cancelled and cannot be edited.");
            }

            var hasBookings = await _dbContext.Reservations.AnyAsync(r => r.ProjectionId == id);
            if (hasBookings)
            {
                throw new ClientException(
                    "This projection already has bookings and cannot be edited. " +
                    "Cancel the projection to refund customers; the sold details stay on record.");
            }

            var movie = await _dbContext.Movies.FindAsync(request.MovieId)
                ?? throw new ClientException($"Movie {request.MovieId} was not found.");

            if (entity.HallId != request.HallId)
            {
                await EnsureHallCanBeScheduledAsync(request.HallId);
            }

            await EnsureLanguageExistsAsync(request.LanguageId);

            var endTime = request.StartTime.AddMinutes(movie.DurationMinutes);
            await ScheduleConflictGuard.EnsureNoHallOverlapAsync(
                _dbContext,
                request.HallId,
                request.StartTime,
                endTime,
                excludeProjectionId: id);

            entity.MovieId = request.MovieId;
            entity.HallId = request.HallId;
            entity.StartTime = request.StartTime;
            entity.BasePrice = request.BasePrice;
            entity.LanguageId = request.LanguageId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();

            return await GetByIdAsync(entity.Id);
        }

        public async Task<ProjectionResponse> CancelAsync(int id, ProjectionCancelRequest? request = null)
        {
            if (request != null)
            {
                await _cancelValidator.ValidateAndThrowAsync(request);
            }

            if (!_userAccessor.HasPermission(RolePermissionNames.ManageProjections))
            {
                throw new ClientException("You do not have permission to cancel a projection.");
            }

            var staffUserId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var projection = await _dbContext.Projections
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Projection with id {id} not found.");

            if (projection.CancelledAt != null)
            {
                // A previous cancel may have marked the show cancelled, then failed while refunding.
                // Finish leftover bookings instead of refusing the retry.
                var leftover = await _reservationService.CancelActiveForProjectionAsync(
                    projection.Id,
                    projection.CancellationReason ?? "Projection cancelled by staff",
                    staffUserId);

                if (leftover.Count == 0)
                {
                    throw new ClientException("This projection is already cancelled.");
                }

                await QueueProjectionCancelledEmailsAsync(leftover, projection, projection.CancellationReason ?? "Projection cancelled by staff");
                await _analyticsNotifier.NotifyAnalyticsChangedAsync();
                return await GetByIdAsync(projection.Id);
            }

            if (projection.StartTime <= DateTime.UtcNow)
            {
                throw new ClientException("A projection that has already started cannot be cancelled.");
            }

            var reason = string.IsNullOrWhiteSpace(request?.Reason)
                ? "Projection cancelled by staff"
                : request!.Reason!.Trim();

            projection.CancelledAt = DateTime.UtcNow;
            projection.CancellationReason = reason;
            projection.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var cancelledBookings = await _reservationService.CancelActiveForProjectionAsync(
                projection.Id,
                reason,
                staffUserId);

            await QueueProjectionCancelledEmailsAsync(cancelledBookings, projection, reason);

            await _analyticsNotifier.NotifyAnalyticsChangedAsync();
            return await GetByIdAsync(projection.Id);
        }

        public async Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id)
        {
            var projection = await _dbContext.Projections
                .AsNoTracking()
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException($"Projection with id {id} not found.");

            var graph = await BookingGraphCascade.CountForProjectionIdsAsync(_dbContext, new[] { id });
            var history = await BookingGraphCascade.CountBookingHistoryAsync(_dbContext, new[] { id });
            var summaries = await BookingGraphCascade.SummarizeBookingsAsync(_dbContext, new[] { id });
            var display = projection.Movie?.Title ?? $"Projection #{id}";

            return BookingGraphCascade.BuildImpact(
                projection.Id,
                display,
                history,
                summaries,
                ("Reservations", graph.ReservationCount),
                ("Reserved seats", graph.ReservationSeatCount));
        }

        /// <summary>
        /// Removes a projection only while it carries no booking or payment history. Once tickets
        /// were sold the record is permanent, so the delete is refused instead of erasing it.
        /// </summary>
        public override async Task DeleteAsync(int id)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var projection = await _dbContext.Projections
                    .Include(s => s.Movie)
                    .FirstOrDefaultAsync(s => s.Id == id)
                    ?? throw new KeyNotFoundException($"Projection with id {id} not found.");

                var display = projection.Movie?.Title is { Length: > 0 } title
                    ? $"Projection of '{title}'"
                    : $"Projection #{id}";

                await BookingGraphCascade.RemoveProjectionsAsync(_dbContext, new[] { id }, display);

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
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

        public async Task<List<ProjectionSeatResponse>> GetSeatsAsync(int projectionId)
        {
            // Seats held for checkouts that were never paid must not look taken on the seat map.
            await _seatHoldService.ReleaseExpiredHoldsAsync(projectionId);

            var projection = await _dbContext.Projections
                .AsNoTracking()
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .Include(s => s.Hall).ThenInclude(h => h.Status)
                .FirstOrDefaultAsync(s => s.Id == projectionId)
                ?? throw new KeyNotFoundException($"Projection with id {projectionId} not found.");

            if (projection.CancelledAt != null)
            {
                throw new ClientException("This projection was cancelled and is no longer on sale.");
            }

            if (projection.Hall.Status?.AllowsProjections == false)
            {
                throw new ClientException("This hall is currently closed and its projections are not on sale.");
            }

            var takenSeatIds = await _dbContext.ReservationSeats
                .Where(rs => rs.ProjectionId == projectionId && rs.ReleasedAt == null)
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
                    var spots = SeatCapacity.PhysicalSpots(s.IsActive, s.SeatType);
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

        private async Task<HashSet<int>> ProjectionIdsWithBookingsAsync(IReadOnlyCollection<int> projectionIds)
        {
            if (projectionIds.Count == 0)
            {
                return new HashSet<int>();
            }

            var ids = await _dbContext.Reservations
                .AsNoTracking()
                .Where(r => projectionIds.Contains(r.ProjectionId))
                .Select(r => r.ProjectionId)
                .Distinct()
                .ToListAsync();
            return ids.ToHashSet();
        }

        private async Task QueueProjectionCancelledEmailsAsync(
            IReadOnlyList<ReservationResponse> bookings,
            Projection projection,
            string reason)
        {
            var movieTitle = projection.Movie?.Title ?? "the movie";
            var hallName = projection.Hall?.Name ?? "the hall";
            var start = CinemaDateTime.FormatLocal(projection.StartTime);

            var byEmail = bookings
                .Select(r => new
                {
                    Booking = r,
                    Email = string.IsNullOrWhiteSpace(r.CustomerEmail) ? null : r.CustomerEmail
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                .GroupBy(x => x.Email!);

            foreach (var group in byEmail)
            {
                var numbers = string.Join("\n", group.Select(x => $"- {x.Booking.ReservationNumber}"));
                try
                {
                    await _emailService.QueueEmailAsync(new EmailMessage
                    {
                        To = group.Key,
                        Subject = $"CineVision: projection cancelled ({movieTitle})",
                        Body =
                            $"Your booking(s) were cancelled because the projection was cancelled by staff.\n\n" +
                            $"Movie: {movieTitle}\n" +
                            $"Hall: {hallName}\n" +
                            $"Start: {start}\n\n" +
                            $"Reservations:\n{numbers}\n\n" +
                            $"{reason}\n\n" +
                            "If you paid online, a refund has been requested.\n\n" +
                            "Thank you,\nCineVision",
                        IsHtml = false
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to queue cancellation email to {Email}.", group.Key);
                }
            }
        }

        private ProjectionResponse MapToResponse(
            Projection s,
            bool includeMovie,
            bool includeHall,
            bool includeSeatStats,
            bool includePoster = false,
            bool hasBookings = false)
        {
            var response = _mapper.Map<ProjectionResponse>(s);
            response.MovieTitle = s.Movie?.Title ?? string.Empty;
            response.MoviePosterBase64 = includePoster ? s.Movie?.PosterImageBase64 : null;
            response.HallName = s.Hall?.Name ?? string.Empty;
            response.IsCancelled = s.CancelledAt != null;
            response.CancelledAt = s.CancelledAt;
            response.CancellationReason = s.CancellationReason;
            response.HasBookings = hasBookings;
            if (s.Movie != null)
            {
                response.EndTime = s.StartTime.AddMinutes(s.Movie.DurationMinutes);
            }

            if (includeSeatStats)
            {
                var totalSeats = s.Hall != null ? SeatCapacity.Of(s.Hall.Seats) : 0;
                var occupied = s.ReservationSeats?.Count(rs => rs.ReleasedAt == null) ?? 0;
                response.TotalSeats = totalSeats;
                response.AvailableSeats = Math.Max(0, totalSeats - occupied);
            }

            if (includeMovie && s.Movie != null)
            {
                response.Movie = _mapper.Map<MovieResponse>(s.Movie);
            }
            if (includeHall && s.Hall != null)
            {
                response.Hall = _mapper.Map<HallResponse>(s.Hall);
                response.Hall.SeatCount = includeSeatStats
                    ? SeatCapacity.Of(s.Hall.Seats)
                    : 0;
                response.Hall.Capacity = response.Hall.SeatCount;
            }

            return response;
        }
    }
}
