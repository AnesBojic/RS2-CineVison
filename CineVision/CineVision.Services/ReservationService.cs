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
using CineVision.Services.ReservationStateMachine;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using CineVision.Model.Enums;

namespace CineVision.Services
{
    public class ReservationService : BaseReadService<Reservation, ReservationResponse, ReservationSearchObject>, IReservationService
    {
        /// <summary>Terminal Stripe PaymentIntent status that means the money actually cleared.</summary>
        private const string StripeSucceededStatus = "succeeded";

        /// <summary>The only currency booking payments are created and accepted in.</summary>
        private const string StripeCurrency = "usd";

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly string _stripeSecretKey;
        private readonly string _stripePublishableKey;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReservationService> _logger;
        private readonly IAnalyticsNotifier _analyticsNotifier;
        private readonly INotificationService _notificationService;
        private readonly IValidator<ReservationCreateRequest> _createValidator;
        private readonly IValidator<CreatePaymentIntentRequest> _paymentIntentValidator;
        private readonly IValidator<ReservationCancelRequest> _cancelValidator;

        public ReservationService(
            CineVisionDbContext dbContext,
            IMapper mapper,
            IAuthenticatedUserAccessor userAccessor,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<ReservationService> logger,
            IAnalyticsNotifier analyticsNotifier,
            INotificationService notificationService,
            IValidator<ReservationCreateRequest> createValidator,
            IValidator<CreatePaymentIntentRequest> paymentIntentValidator,
            IValidator<ReservationCancelRequest> cancelValidator)
            : base(mapper, dbContext)
        {
            _userAccessor = userAccessor;
            _stripeSecretKey = configuration["Stripe:SecretKey"]
                ?? throw new InvalidOperationException("Stripe secret key is not configured.");
            _stripePublishableKey = configuration["Stripe:PublishableKey"]
                ?? throw new InvalidOperationException("Stripe publishable key is not configured.");
            _emailService = emailService;
            _logger = logger;
            _analyticsNotifier = analyticsNotifier;
            _notificationService = notificationService;
            _createValidator = createValidator;
            _paymentIntentValidator = paymentIntentValidator;
            _cancelValidator = cancelValidator;
        }

        private bool IsAdminOrStaff() =>
            _userAccessor.IsInRole(RoleNames.Admin) || _userAccessor.IsInRole(RoleNames.Staff);

        protected override IQueryable<Reservation> ApplyFilters(IQueryable<Reservation> query, ReservationSearchObject? search)
        {
            return query;
        }

        public override async Task<PageResult<ReservationResponse>> GetAllAsync(ReservationSearchObject? search = null)
        {
            search ??= new ReservationSearchObject();
            PagingLimits.Normalize(search);

            var userId = _userAccessor.GetUserId();
            if (!userId.HasValue)
            {
                return new PageResult<ReservationResponse> { Items = new List<ReservationResponse>(), TotalCount = 0 };
            }

            IQueryable<Reservation> query = _dbContext.Reservations
                .AsNoTracking()
                .Include(r => r.Projection).ThenInclude(s => s.Movie)
                .Include(r => r.Projection).ThenInclude(s => s.Hall)
                .Include(r => r.ReservationSeats).ThenInclude(rs => rs.Seat);

            // Customers see only their bookings; Admin/Staff can manage all.
            if (!IsAdminOrStaff())
            {
                query = query.Where(r => r.UserId == userId.Value);
            }

            if (search.Status.HasValue)
            {
                query = query.Where(r => (int)r.Status == search.Status.Value);
            }
            if (search.ProjectionId.HasValue)
            {
                query = query.Where(r => r.ProjectionId == search.ProjectionId.Value);
            }

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = await query.CountAsync();
            }

            query = query.OrderByDescending(r => r.ReservationDate)
                .Skip((search.Page!.Value - 1) * search.PageSize!.Value)
                .Take(search.PageSize.Value);

            var entities = await query.ToListAsync();

            return new PageResult<ReservationResponse>
            {
                Items = entities.Select(MapToResponse).ToList(),
                TotalCount = totalCount
            };
        }

        public override async Task<ReservationResponse> GetByIdAsync(int id)
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new KeyNotFoundException($"Reservation with id {id} not found.");

            var reservation = await LoadReservationForReadAsync(id, userId)
                ?? throw new KeyNotFoundException($"Reservation with id {id} not found.");

            return MapToResponse(reservation);
        }

        private async Task<Reservation?> LoadReservationForReadAsync(int id, int userId)
        {
            var query = _dbContext.Reservations
                .AsNoTracking()
                .Include(r => r.Projection).ThenInclude(s => s.Movie)
                .Include(r => r.Projection).ThenInclude(s => s.Hall)
                .Include(r => r.ReservationSeats).ThenInclude(rs => rs.Seat)
                .Where(r => r.Id == id);

            if (!IsAdminOrStaff())
            {
                query = query.Where(r => r.UserId == userId);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<ReservationResponse> CreateReservationAsync(ReservationCreateRequest request)
        {
            await _createValidator.ValidateAndThrowAsync(request);

            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var seatIds = request.SeatIds.Distinct().ToList();

            var paymentIntentId = string.IsNullOrWhiteSpace(request.PaymentIntentId)
                ? null
                : request.PaymentIntentId.Trim();

            // Idempotent confirm: same PaymentIntent already booked → return existing reservation.
            if (paymentIntentId != null)
            {
                var existing = await FindByPaymentIntentAsync(paymentIntentId);
                if (existing != null)
                {
                    if (existing.UserId != userId && !IsAdminOrStaff())
                    {
                        throw new ClientException("This payment was already used for another booking.");
                    }

                    return await GetByIdAsync(existing.Id);
                }
            }

            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var projection = await _dbContext.Projections
                    .Include(s => s.Hall).ThenInclude(h => h.Seats)
                    .FirstOrDefaultAsync(s => s.Id == request.ProjectionId)
                    ?? throw new ClientException($"Projection {request.ProjectionId} was not found.");

                if (!projection.IsActive)
                {
                    throw new ClientException("This projection is not available for booking.");
                }

                if (projection.StartTime <= DateTime.UtcNow)
                {
                    throw new ClientException("This projection has already started.");
                }

                var hallSeats = projection.Hall.Seats.ToDictionary(s => s.Id);
                var expandedSeatIds = new HashSet<int>();
                foreach (var seatId in seatIds)
                {
                    if (!hallSeats.TryGetValue(seatId, out var seat) || !seat.IsActive)
                    {
                        throw new ClientException($"Seat {seatId} does not belong to this projection's hall or is not available.");
                    }

                    expandedSeatIds.Add(seatId);
                    if (seat.SeatType == SeatType.Couple)
                    {
                        if (!seat.PartnerSeatId.HasValue)
                        {
                            throw new ClientException($"Couple seat {seat.RowLabel}{seat.SeatNumber} is not configured correctly.");
                        }

                        expandedSeatIds.Add(seat.PartnerSeatId.Value);
                    }
                }

                var expandedList = expandedSeatIds.ToList();

                var alreadyTaken = await _dbContext.ReservationSeats
                    .Where(rs => rs.ProjectionId == projection.Id && expandedList.Contains(rs.SeatId))
                    .AnyAsync();

                if (alreadyTaken)
                {
                    throw new ClientException("One or more of the selected seats are already reserved.");
                }

                var total = projection.BasePrice * expandedList.Count;
                var initialStatus = ReservationStatus.Confirmed;

                if (paymentIntentId != null)
                {
                    await VerifyStripePaymentSucceededAsync(
                        paymentIntentId,
                        expectedAmountCents: (long)(total * 100),
                        expectedProjectionId: projection.Id,
                        expectedUserId: userId);
                    initialStatus = ReservationStatus.Paid;
                }

                if (!ReservationStatusTransitions.IsValidInitialStatus(initialStatus))
                {
                    throw new ClientException($"Invalid initial reservation status: {initialStatus}.");
                }

                var reservation = new Reservation
                {
                    UserId = userId,
                    ProjectionId = projection.Id,
                    ReservationDate = DateTime.UtcNow,
                    ReservationNumber = $"R-{DateTime.UtcNow:yyyyMMddHHmmss}-{userId}",
                    Status = initialStatus,
                    TotalAmount = total,
                    CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim(),
                    CustomerEmail = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim(),
                    PaymentTransactionId = paymentIntentId,
                    PaymentDate = initialStatus == ReservationStatus.Paid ? DateTime.UtcNow : null
                };

                foreach (var seatId in expandedList)
                {
                    reservation.ReservationSeats.Add(new ReservationSeat
                    {
                        SeatId = seatId,
                        ProjectionId = projection.Id,
                        Price = projection.BasePrice
                    });
                }

                _dbContext.Reservations.Add(reservation);

                try
                {
                    await _dbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (DbUpdateException) when (paymentIntentId != null)
                {
                    // Race: another request won the unique PaymentTransactionId index.
                    await tx.RollbackAsync();
                    var raced = await FindByPaymentIntentAsync(paymentIntentId);
                    if (raced != null && (raced.UserId == userId || IsAdminOrStaff()))
                    {
                        return await GetByIdAsync(raced.Id);
                    }

                    throw new ClientException("This payment was already used for another booking.");
                }

                var response = await GetByIdAsync(reservation.Id);

                // Queue a confirmation email; a queue/broker outage must never fail the reservation.
                await SendConfirmationEmailAsync(reservation, response);
                await NotifyBookingCreatedSafeAsync(response);
                await NotifyAnalyticsSafeAsync();

                return response;
            }
            catch (ClientException)
            {
                try { await tx.RollbackAsync(); } catch { /* already committed/rolled back */ }
                throw;
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { /* already committed/rolled back */ }
                throw;
            }
        }

        private async Task<Reservation?> FindByPaymentIntentAsync(string paymentIntentId)
        {
            return await _dbContext.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.PaymentTransactionId == paymentIntentId);
        }

        /// <summary>
        /// Confirms with Stripe that the PaymentIntent succeeded and matches the server-calculated amount.
        /// </summary>
        private async Task VerifyStripePaymentSucceededAsync(
            string paymentIntentId,
            long expectedAmountCents,
            int expectedProjectionId,
            int expectedUserId)
        {
            ConfigureStripe();

            PaymentIntent intent;
            try
            {
                var service = new PaymentIntentService();
                intent = await service.GetAsync(paymentIntentId);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe lookup failed for PaymentIntent {PaymentIntentId}.", paymentIntentId);
                throw new ClientException(
                    ex.StripeError?.Message ?? "Could not verify payment with Stripe. Please try again.");
            }

            if (!string.Equals(intent.Status, StripeSucceededStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new ClientException(
                    $"Payment is not completed (status: {intent.Status}). Complete payment before confirming the booking.");
            }

            if (intent.Amount != expectedAmountCents)
            {
                throw new ClientException(
                    "Paid amount does not match the booking total. Payment was not accepted for these seats.");
            }

            if (!string.Equals(intent.Currency, StripeCurrency, StringComparison.OrdinalIgnoreCase))
            {
                throw new ClientException("Unexpected payment currency.");
            }

            // Metadata is set when the intent is created; reject mismatched intents.
            if (intent.Metadata != null)
            {
                if (intent.Metadata.TryGetValue("projectionId", out var metaProjection) &&
                    int.TryParse(metaProjection, out var projectionId) &&
                    projectionId != expectedProjectionId)
                {
                    throw new ClientException("Payment was created for a different projection.");
                }

                if (intent.Metadata.TryGetValue("userId", out var metaUser) &&
                    int.TryParse(metaUser, out var metaUserId) &&
                    metaUserId != expectedUserId)
                {
                    throw new ClientException("Payment belongs to a different user.");
                }
            }
        }

        private void ConfigureStripe()
        {
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        private async Task SendConfirmationEmailAsync(Reservation reservation, ReservationResponse response)
        {
            try
            {
                var to = reservation.CustomerEmail;
                if (string.IsNullOrWhiteSpace(to))
                {
                    to = await _dbContext.Users
                        .Where(u => u.Id == reservation.UserId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();
                }

                if (string.IsNullOrWhiteSpace(to))
                {
                    _logger.LogWarning("Reservation {ReservationId} has no email address; confirmation not sent.", reservation.Id);
                    return;
                }

                var seats = string.Join(", ", response.Seats.Select(s => $"{s.RowLabel}{s.SeatNumber}"));

                var body =
                    $"Your booking is confirmed!\n\n" +
                    $"Reservation: {response.ReservationNumber}\n" +
                    $"Movie: {response.MovieTitle}\n" +
                    $"Hall: {response.HallName}\n" +
                    $"Start: {CinemaDateTime.FormatLocal(response.ProjectionStartTime)}\n" +
                    $"Seats: {seats}\n" +
                    $"Total: {response.TotalAmount:0.00}\n\n" +
                    $"Thank you for booking with CineVision.";

                await _emailService.QueueEmailAsync(new EmailMessage
                {
                    To = to,
                    Subject = $"CineVision booking confirmation {response.ReservationNumber}",
                    Body = body,
                    IsHtml = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue confirmation email for reservation {ReservationId}.", reservation.Id);
            }
        }

        public async Task<ReservationResponse> CancelAsync(int id, ReservationCancelRequest? request = null)
        {
            if (request != null)
            {
                await _cancelValidator.ValidateAndThrowAsync(request);
            }

            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var isStaff = IsAdminOrStaff();

            var reservation = await _dbContext.Reservations
                .Include(r => r.ReservationSeats)
                .Include(r => r.Projection)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Reservation with id {id} not found.");

            if (!isStaff && reservation.UserId != userId)
            {
                throw new KeyNotFoundException($"Reservation with id {id} not found.");
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                throw new ClientException("This reservation is already cancelled.");
            }

            ReservationStatusTransitions.EnsureCanTransition(reservation.Status, ReservationStatus.Cancelled);

            // Customers must cancel at least 4h before showtime; Admin/Staff may cancel anytime.
            if (!isStaff && reservation.Projection.StartTime <= DateTime.UtcNow.AddHours(4))
            {
                throw new ClientException(
                    "Tickets can only be refunded at least 4 hours before the projection starts.");
            }

            // Paid Stripe bookings: refund money before freeing seats.
            if (reservation.Status == ReservationStatus.Paid &&
                !string.IsNullOrWhiteSpace(reservation.PaymentTransactionId))
            {
                await RefundStripePaymentAsync(reservation.PaymentTransactionId);
            }

            var reason = string.IsNullOrWhiteSpace(request?.Reason)
                ? (isStaff ? "Cancelled by staff" : "Cancelled by customer")
                : request!.Reason!.Trim();

            ReservationStatusTransitions.Apply(
                reservation,
                ReservationStatus.Cancelled,
                cancelledByUserId: userId,
                cancellationReason: reason);

            // Free the seats so they become available again for the projection.
            // Analytics read from ReservationSeats, so occupancy/revenue update automatically.
            _dbContext.ReservationSeats.RemoveRange(reservation.ReservationSeats);
            await _dbContext.SaveChangesAsync();

            await NotifySafeAsync(
                reservation.UserId,
                "Booking cancelled",
                $"Reservation {reservation.ReservationNumber} was cancelled. {reason}",
                NotificationType.Cancellation);

            await NotifyAnalyticsSafeAsync();

            return await GetByIdAsync(reservation.Id);
        }

        public async Task<ReservationResponse> CompleteAsync(int id)
        {
            if (!IsAdminOrStaff())
            {
                throw new ClientException("Only Admin or Staff can mark a reservation as completed.");
            }

            var reservation = await _dbContext.Reservations
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Reservation with id {id} not found.");

            ReservationStatusTransitions.Apply(reservation, ReservationStatus.Completed);
            await _dbContext.SaveChangesAsync();

            await NotifySafeAsync(
                reservation.UserId,
                "Booking completed",
                $"Reservation {reservation.ReservationNumber} is marked as completed. Thanks for visiting CineVision!",
                NotificationType.Status);

            await NotifyAnalyticsSafeAsync();

            return await GetByIdAsync(reservation.Id);
        }

        private async Task RefundStripePaymentAsync(string paymentIntentId)
        {
            ConfigureStripe();

            try
            {
                var refundService = new RefundService();
                await refundService.CreateAsync(new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe refund failed for PaymentIntent {PaymentIntentId}.", paymentIntentId);
                throw new ClientException(
                    ex.StripeError?.Message ?? "Payment refund failed. Please try again or contact support.");
            }
        }

        public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
        {
            await _paymentIntentValidator.ValidateAndThrowAsync(request);

            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var seatIds = request.SeatIds.Distinct().ToList();

            var projection = await _dbContext.Projections
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .FirstOrDefaultAsync(s => s.Id == request.ProjectionId)
                ?? throw new ClientException($"Projection {request.ProjectionId} was not found.");

            if (!projection.IsActive)
            {
                throw new ClientException("This projection is not available for booking.");
            }

            var hallSeats = projection.Hall.Seats.ToDictionary(s => s.Id);
            var expandedCount = 0;
            foreach (var seatId in seatIds)
            {
                if (!hallSeats.TryGetValue(seatId, out var seat) || !seat.IsActive)
                {
                    throw new ClientException($"Seat {seatId} does not belong to this projection's hall or is not available.");
                }

                expandedCount += seat.SeatType == SeatType.Couple ? 2 : 1;
            }

            var total = projection.BasePrice * expandedCount;
            var amountCents = (long)(total * 100);

            ConfigureStripe();
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountCents,
                Currency = StripeCurrency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                Metadata = new Dictionary<string, string>
                {
                    ["projectionId"] = projection.Id.ToString(),
                    ["userId"] = userId.ToString(),
                    ["seatCount"] = expandedCount.ToString()
                }
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            return new PaymentIntentResponse
            {
                PaymentIntentId = intent.Id,
                ClientSecret = intent.ClientSecret ?? string.Empty,
                PublishableKey = _stripePublishableKey
            };
        }

        private async Task NotifyAnalyticsSafeAsync()
        {
            try
            {
                await _analyticsNotifier.NotifyAnalyticsChangedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to push live analytics update.");
            }
        }

        private async Task NotifyBookingCreatedSafeAsync(ReservationResponse response)
        {
            if (response.Status == (int)ReservationStatus.Paid)
            {
                await NotifySafeAsync(
                    response.UserId,
                    "Payment confirmed",
                    $"Payment received for {response.ReservationNumber} — {response.MovieTitle}. Seats are reserved.",
                    NotificationType.Payment);
            }
            else
            {
                await NotifySafeAsync(
                    response.UserId,
                    "Booking confirmed",
                    $"Reservation {response.ReservationNumber} for {response.MovieTitle} is confirmed.",
                    NotificationType.Reservation);
            }
        }

        private async Task NotifySafeAsync(int userId, string title, string message, NotificationType type)
        {
            try
            {
                await _notificationService.CreateAsync(userId, title, message, type);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create {Type} notification for user {UserId}.", type, userId);
            }
        }

        private ReservationResponse MapToResponse(Reservation r)
        {
            return new ReservationResponse
            {
                Id = r.Id,
                ReservationNumber = r.ReservationNumber,
                ReservationDate = r.ReservationDate,
                Status = (int)r.Status,
                StatusName = r.Status.ToString(),
                TotalAmount = r.TotalAmount,
                UserId = r.UserId,
                CustomerName = r.CustomerName,
                CustomerEmail = r.CustomerEmail,
                ProjectionId = r.ProjectionId,
                MovieId = r.Projection?.MovieId ?? 0,
                MovieTitle = r.Projection?.Movie?.Title ?? string.Empty,
                HallName = r.Projection?.Hall?.Name ?? string.Empty,
                ProjectionStartTime = r.Projection?.StartTime ?? default,
                ProjectionEndTime = r.Projection?.EndTime ?? default,
                PaymentTransactionId = r.PaymentTransactionId,
                PaymentDate = r.PaymentDate,
                CancelledByUserId = r.CancelledByUserId,
                CancelledAt = r.CancelledAt,
                CancellationReason = r.CancellationReason,
                CompletedAt = r.CompletedAt,
                Seats = r.ReservationSeats
                    .OrderBy(rs => rs.Seat != null ? rs.Seat.RowLabel : string.Empty)
                    .ThenBy(rs => rs.Seat != null ? rs.Seat.SeatNumber : 0)
                    .Select(rs => new ReservationSeatResponse
                    {
                        Id = rs.Id,
                        SeatId = rs.SeatId,
                        RowLabel = rs.Seat?.RowLabel ?? string.Empty,
                        SeatNumber = rs.Seat?.SeatNumber ?? 0,
                        SeatType = (int)(rs.Seat?.SeatType ?? SeatType.Regular),
                        Price = rs.Price
                    })
                    .ToList()
            };
        }
    }
}
