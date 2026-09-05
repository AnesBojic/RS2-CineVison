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
// Stripe ships its own PaymentMethod type; bookings always mean the domain enum.
using PaymentMethod = CineVision.Model.Enums.PaymentMethod;

namespace CineVision.Services
{
    public class ReservationService : BaseReadService<Reservation, ReservationResponse, ReservationSearchObject>, IReservationService
    {
        /// <summary>Terminal Stripe PaymentIntent status that means the money actually cleared.</summary>
        private const string StripeSucceededStatus = "succeeded";

        /// <summary>The only currency booking payments are created and accepted in.</summary>
        private const string StripeCurrency = "usd";

        /// <summary>Fallback checkout window when Booking:SeatHoldMinutes is not configured.</summary>
        private const int DefaultSeatHoldMinutes = 15;

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly ISeatHoldService _seatHoldService;
        private readonly int _seatHoldMinutes;
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
            ISeatHoldService seatHoldService,
            IValidator<ReservationCreateRequest> createValidator,
            IValidator<CreatePaymentIntentRequest> paymentIntentValidator,
            IValidator<ReservationCancelRequest> cancelValidator)
            : base(mapper, dbContext)
        {
            _userAccessor = userAccessor;
            _seatHoldService = seatHoldService;
            _seatHoldMinutes = int.TryParse(configuration["Booking:SeatHoldMinutes"], out var holdMinutes) && holdMinutes > 0
                ? holdMinutes
                : DefaultSeatHoldMinutes;
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

        /// <summary>
        /// Finalises a booking. Online bookings may only confirm a seat hold whose Stripe payment
        /// succeeded; a booking without an online payment must be an explicit counter sale.
        /// </summary>
        public async Task<ReservationResponse> CreateReservationAsync(ReservationCreateRequest request)
        {
            await _createValidator.ValidateAndThrowAsync(request);

            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var paymentMethod = request.PaymentMethod ?? PaymentMethod.Online;

            return paymentMethod == PaymentMethod.Counter
                ? await CreateCounterReservationAsync(request, userId)
                : await FinalizeOnlineReservationAsync(request, userId);
        }

        /// <summary>
        /// Turns the pre-paid seat hold into a Paid booking. The hold — not the request — is the
        /// source of truth for projection, seats and amount, so a client cannot re-price itself.
        /// </summary>
        private async Task<ReservationResponse> FinalizeOnlineReservationAsync(ReservationCreateRequest request, int userId)
        {
            var paymentIntentId = request.PaymentIntentId?.Trim();
            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                throw new ClientException("Online bookings must be paid before they can be confirmed.");
            }

            var reservation = await _dbContext.Reservations
                .Include(r => r.ReservationSeats)
                .FirstOrDefaultAsync(r => r.PaymentTransactionId == paymentIntentId)
                ?? throw new ClientException("No booking is waiting for this payment. Please start the checkout again.");

            if (reservation.UserId != userId && !IsAdminOrStaff())
            {
                throw new ClientException("This payment belongs to another customer.");
            }

            // Retried confirm (flaky network, app restart): the booking is already finalised.
            if (reservation.Status == ReservationStatus.Paid)
            {
                return await GetByIdAsync(reservation.Id);
            }

            if (reservation.Status != ReservationStatus.Pending)
            {
                throw new ClientException($"This booking can no longer be paid (status: {reservation.Status}).");
            }

            EnsureRequestMatchesHold(request, reservation);

            await VerifyStripePaymentSucceededAsync(paymentIntentId, reservation);

            ReservationStatusTransitions.Apply(reservation, ReservationStatus.Paid);
            // Permanent record that money cleared; later lifecycle changes must not erase it.
            reservation.PaymentStatus = PaymentStatus.Paid;
            reservation.HoldExpiresAt = null;

            if (!string.IsNullOrWhiteSpace(request.CustomerName))
            {
                reservation.CustomerName = request.CustomerName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                reservation.CustomerEmail = request.CustomerEmail.Trim();
            }

            await _dbContext.SaveChangesAsync();

            var response = await GetByIdAsync(reservation.Id);

            // Queue a confirmation email; a queue/broker outage must never fail the reservation.
            await SendConfirmationEmailAsync(reservation, response);
            await NotifyBookingCreatedSafeAsync(response);
            await NotifyAnalyticsSafeAsync();

            return response;
        }

        /// <summary>
        /// Box-office sale: the only booking that is valid without a Stripe payment, and only
        /// Admin or Staff may register one.
        /// </summary>
        private async Task<ReservationResponse> CreateCounterReservationAsync(ReservationCreateRequest request, int userId)
        {
            if (!IsAdminOrStaff())
            {
                throw new ClientException("Only Admin or Staff can register a booking paid at the counter.");
            }

            await _seatHoldService.ReleaseExpiredHoldsAsync(request.ProjectionId);

            var quote = await BuildQuoteAsync(request.ProjectionId, request.SeatIds);

            var reservation = new Reservation
            {
                UserId = userId,
                ProjectionId = quote.Projection.Id,
                ReservationDate = DateTime.UtcNow,
                ReservationNumber = BuildReservationNumber(userId),
                Status = ReservationStatus.Confirmed,
                PaymentMethod = PaymentMethod.Counter,
                TotalAmount = quote.Total,
                CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim(),
                CustomerEmail = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim()
            };

            AddSeats(reservation, quote);
            _dbContext.Reservations.Add(reservation);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Lost the race against the unique (ProjectionId, SeatId) index.
                throw new ClientException("One or more of the selected seats are already reserved.");
            }

            var response = await GetByIdAsync(reservation.Id);
            await SendConfirmationEmailAsync(reservation, response);
            await NotifyBookingCreatedSafeAsync(response);
            await NotifyAnalyticsSafeAsync();

            return response;
        }

        /// <summary>
        /// A finalise call must be for the hold it was quoted, otherwise the client is confirming
        /// a payment against seats it was never priced for.
        /// </summary>
        private static void EnsureRequestMatchesHold(ReservationCreateRequest request, Reservation hold)
        {
            if (request.ProjectionId != hold.ProjectionId)
            {
                throw new ClientException("This payment was created for a different projection.");
            }

            var heldSeatIds = hold.ReservationSeats.Select(rs => rs.SeatId).ToHashSet();
            if (request.SeatIds.Distinct().Any(seatId => !heldSeatIds.Contains(seatId)))
            {
                throw new ClientException("This payment was created for a different set of seats.");
            }
        }

        private static string BuildReservationNumber(int userId) =>
            $"R-{DateTime.UtcNow:yyyyMMddHHmmss}-{userId}";

        private static void AddSeats(Reservation reservation, BookingQuote quote)
        {
            foreach (var seatId in quote.SeatIds)
            {
                reservation.ReservationSeats.Add(new ReservationSeat
                {
                    SeatId = seatId,
                    ProjectionId = quote.Projection.Id,
                    Price = quote.Projection.BasePrice
                });
            }
        }

        /// <summary>Server-side result of validating and pricing a seat selection.</summary>
        private sealed record BookingQuote(Projection Projection, List<int> SeatIds, decimal Total)
        {
            public long AmountCents => (long)Math.Round(Total * 100m, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// The single gate both charging and finalising go through: the projection must still be
        /// open, every seat must exist and be free, couple seats expand to their partner, and the
        /// price is always recomputed from BasePrice instead of trusting the client.
        /// </summary>
        private async Task<BookingQuote> BuildQuoteAsync(int projectionId, IEnumerable<int> requestedSeatIds)
        {
            var projection = await _dbContext.Projections
                .Include(s => s.Hall).ThenInclude(h => h.Seats)
                .Include(s => s.Hall).ThenInclude(h => h.Status)
                .FirstOrDefaultAsync(s => s.Id == projectionId)
                ?? throw new ClientException($"Projection {projectionId} was not found.");

            if (projection.StartTime <= DateTime.UtcNow)
            {
                throw new ClientException("This projection has already started.");
            }

            if (projection.CancelledAt != null)
            {
                throw new ClientException("This projection was cancelled and is no longer on sale.");
            }

            // A hall can only be closed while it has no upcoming shows, but rows saved before that
            // rule existed can still be out of sync; refuse to sell into a closed hall either way.
            if (projection.Hall.Status?.AllowsProjections == false)
            {
                throw new ClientException("This hall is currently closed and its projections are not on sale.");
            }

            var hallSeats = projection.Hall.Seats.ToDictionary(s => s.Id);
            var expandedSeatIds = new HashSet<int>();

            foreach (var seatId in requestedSeatIds.Distinct())
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
                .AnyAsync(rs => rs.ProjectionId == projection.Id
                                && rs.ReleasedAt == null
                                && expandedList.Contains(rs.SeatId));

            if (alreadyTaken)
            {
                throw new ClientException("One or more of the selected seats are already reserved.");
            }

            return new BookingQuote(projection, expandedList, projection.BasePrice * expandedList.Count);
        }

        /// <summary>
        /// Confirms with Stripe that the PaymentIntent succeeded and belongs to exactly this hold.
        /// Every check is fail-closed: anything missing, unparsable or mismatched rejects the booking.
        /// </summary>
        private async Task VerifyStripePaymentSucceededAsync(string paymentIntentId, Reservation hold)
        {
            var expectedAmountCents = (long)Math.Round(hold.TotalAmount * 100m, MidpointRounding.AwayFromZero);

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

            // Metadata is written when the intent is created. Missing or unreadable metadata is
            // treated as a mismatch, so an intent from anywhere else can never confirm a booking.
            if (intent.Metadata == null)
            {
                throw new ClientException("Payment cannot be matched to this booking.");
            }

            RequireMetadataMatch(intent.Metadata, "reservationId", hold.Id, "booking");
            RequireMetadataMatch(intent.Metadata, "projectionId", hold.ProjectionId, "projection");
            RequireMetadataMatch(intent.Metadata, "userId", hold.UserId, "customer");
        }

        private static void RequireMetadataMatch(
            IDictionary<string, string> metadata,
            string key,
            int expected,
            string subject)
        {
            if (!metadata.TryGetValue(key, out var raw) ||
                !int.TryParse(raw, out var actual) ||
                actual != expected)
            {
                throw new ClientException($"Payment was not created for this {subject}.");
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
                // The cancellation stands, but the money may still be owed: either the outcome was
                // never saved (Pending) or Stripe rejected the refund (Failed). Cancelling again
                // retries the refund and records the real result instead of erroring out.
                var refundUnresolved =
                    reservation.RefundStatus is RefundStatus.Pending or RefundStatus.Failed;

                if (refundUnresolved && !string.IsNullOrWhiteSpace(reservation.PaymentTransactionId))
                {
                    await RefundAndRecordAsync(reservation);
                    await _dbContext.SaveChangesAsync();
                    return await GetByIdAsync(reservation.Id);
                }

                throw new ClientException("This reservation is already cancelled.");
            }

            ReservationStatusTransitions.EnsureCanTransition(reservation.Status, ReservationStatus.Cancelled);

            // Customers must cancel at least 4h before showtime; Admin/Staff may cancel anytime.
            if (!isStaff && reservation.Projection.StartTime <= DateTime.UtcNow.AddHours(4))
            {
                throw new ClientException(
                    "Tickets can only be refunded at least 4 hours before the projection starts.");
            }

            var reason = string.IsNullOrWhiteSpace(request?.Reason)
                ? (isStaff ? "Cancelled by staff" : "Cancelled by customer")
                : request!.Reason!.Trim();

            ReservationStatusTransitions.Apply(
                reservation,
                ReservationStatus.Cancelled,
                cancelledByUserId: userId,
                cancellationReason: reason);

            // Seats are released, not deleted: availability frees up while the exact seats and
            // prices that were bought stay on record.
            ReleaseSeats(reservation);

            // The refund owed is committed before Stripe is called, so a refund that fails or
            // never returns cannot leave the database claiming nothing was owed.
            var refundOwed =
                reservation.PaymentStatus == PaymentStatus.Paid &&
                reservation.RefundStatus == RefundStatus.None &&
                !string.IsNullOrWhiteSpace(reservation.PaymentTransactionId);

            if (refundOwed)
            {
                reservation.RefundStatus = RefundStatus.Pending;
            }

            await _dbContext.SaveChangesAsync();

            if (refundOwed)
            {
                await RefundAndRecordAsync(reservation);
                await _dbContext.SaveChangesAsync();
            }

            await NotifySafeAsync(
                reservation.UserId,
                "Booking cancelled",
                $"Reservation {reservation.ReservationNumber} was cancelled. {reason}",
                NotificationType.Cancellation);

            await NotifyAnalyticsSafeAsync();

            return await GetByIdAsync(reservation.Id);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ReservationResponse>> CancelActiveForProjectionAsync(
            int projectionId,
            string reason,
            int cancelledByUserId)
        {
            var reservations = await _dbContext.Reservations
                .Include(r => r.ReservationSeats).ThenInclude(rs => rs.Seat)
                .Include(r => r.Projection).ThenInclude(p => p.Movie)
                .Include(r => r.Projection).ThenInclude(p => p.Hall)
                .Include(r => r.User)
                .Where(r => r.ProjectionId == projectionId
                            && r.Status != ReservationStatus.Completed)
                .ToListAsync();

            var active = reservations
                .Where(r => r.Status != ReservationStatus.Cancelled)
                .ToList();
            var refundRetry = reservations
                .Where(r => r.Status == ReservationStatus.Cancelled
                            && r.RefundStatus is RefundStatus.Pending or RefundStatus.Failed
                            && !string.IsNullOrWhiteSpace(r.PaymentTransactionId))
                .ToList();

            if (active.Count == 0 && refundRetry.Count == 0)
            {
                return Array.Empty<ReservationResponse>();
            }

            foreach (var reservation in active)
            {
                ReservationStatusTransitions.EnsureCanTransition(reservation.Status, ReservationStatus.Cancelled);
                ReservationStatusTransitions.Apply(
                    reservation,
                    ReservationStatus.Cancelled,
                    cancelledByUserId: cancelledByUserId,
                    cancellationReason: reason);
                ReleaseSeats(reservation);

                var refundOwed =
                    reservation.PaymentStatus == PaymentStatus.Paid &&
                    reservation.RefundStatus == RefundStatus.None &&
                    !string.IsNullOrWhiteSpace(reservation.PaymentTransactionId);

                if (refundOwed)
                {
                    reservation.RefundStatus = RefundStatus.Pending;
                    refundRetry.Add(reservation);
                }
            }

            await _dbContext.SaveChangesAsync();

            foreach (var reservation in refundRetry.Distinct())
            {
                await RefundAndRecordAsync(reservation);
            }

            await _dbContext.SaveChangesAsync();

            var notify = active;
            var responses = new List<ReservationResponse>(active.Count + refundRetry.Count);
            foreach (var reservation in active.Concat(refundRetry).Distinct())
            {
                responses.Add(MapToResponse(reservation));
            }

            foreach (var reservation in notify)
            {
                await NotifySafeAsync(
                    reservation.UserId,
                    "Projection cancelled",
                    $"Reservation {reservation.ReservationNumber} was cancelled because the projection was cancelled. {reason}",
                    NotificationType.Cancellation);
            }

            await NotifyAnalyticsSafeAsync();
            return responses;
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

        /// <summary>Marks every still-occupied seat of a booking as released.</summary>
        private static void ReleaseSeats(Reservation reservation)
        {
            var now = DateTime.UtcNow;
            foreach (var seat in reservation.ReservationSeats.Where(rs => rs.ReleasedAt == null))
            {
                seat.ReleasedAt = now;
            }
        }

        /// <summary>
        /// Refunds the booking and writes the outcome onto it. A failed refund keeps the
        /// cancellation and records the error instead of being swallowed.
        /// </summary>
        private async Task RefundAndRecordAsync(Reservation reservation)
        {
            ConfigureStripe();

            try
            {
                var refund = await new RefundService().CreateAsync(new RefundCreateOptions
                {
                    PaymentIntent = reservation.PaymentTransactionId,
                });

                reservation.RefundStatus = RefundStatus.Refunded;
                reservation.RefundId = refund.Id;
                reservation.RefundedAt = DateTime.UtcNow;
                reservation.RefundError = null;
            }
            catch (StripeException ex)
            {
                if (await TryRecordExistingRefundAsync(reservation, ex))
                {
                    return;
                }

                reservation.RefundStatus = RefundStatus.Failed;
                reservation.RefundError = Truncate(
                    ex.StripeError?.Message ?? ex.Message ?? "Stripe refund failed.",
                    500);

                _logger.LogError(
                    ex,
                    "Stripe refund failed for reservation {ReservationId} (PaymentIntent {PaymentIntentId}); recorded as {RefundStatus}.",
                    reservation.Id,
                    reservation.PaymentTransactionId,
                    RefundStatus.Failed);
            }
        }

        /// <summary>
        /// When Stripe already refunded the charge (retry after a save failure), copy that refund
        /// onto the booking instead of recording a false failure.
        /// </summary>
        private async Task<bool> TryRecordExistingRefundAsync(Reservation reservation, StripeException ex)
        {
            var code = ex.StripeError?.Code;
            if (!string.Equals(code, "charge_already_refunded", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var existing = await new RefundService().ListAsync(new RefundListOptions
            {
                PaymentIntent = reservation.PaymentTransactionId,
                Limit = 1
            });
            var refund = existing.Data.FirstOrDefault();
            if (refund == null)
            {
                return false;
            }

            reservation.RefundStatus = RefundStatus.Refunded;
            reservation.RefundId = refund.Id;
            reservation.RefundedAt = refund.Created != default
                ? refund.Created
                : DateTime.UtcNow;
            reservation.RefundError = null;
            return true;
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];

        /// <summary>
        /// Prepares a payment: the seats are validated, priced and held as a Pending reservation
        /// before Stripe is asked for money, so a customer can never be charged for a booking the
        /// server would reject afterwards.
        /// </summary>
        public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
        {
            await _paymentIntentValidator.ValidateAndThrowAsync(request);

            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            // Free seats stuck behind abandoned checkouts before pricing this one.
            await _seatHoldService.ReleaseExpiredHoldsAsync(request.ProjectionId);
            await _seatHoldService.ReleaseOwnHoldsAsync(userId, request.ProjectionId);

            var quote = await BuildQuoteAsync(request.ProjectionId, request.SeatIds);

            var hold = new Reservation
            {
                UserId = userId,
                ProjectionId = quote.Projection.Id,
                ReservationDate = DateTime.UtcNow,
                ReservationNumber = BuildReservationNumber(userId),
                Status = ReservationStatus.Pending,
                PaymentMethod = PaymentMethod.Online,
                TotalAmount = quote.Total,
                HoldExpiresAt = DateTime.UtcNow.AddMinutes(_seatHoldMinutes)
            };

            AddSeats(hold, quote);
            _dbContext.Reservations.Add(hold);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Lost the race against the unique (ProjectionId, SeatId) index.
                throw new ClientException("One or more of the selected seats are already reserved.");
            }

            PaymentIntent intent;
            try
            {
                ConfigureStripe();
                intent = await new PaymentIntentService().CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = quote.AmountCents,
                    Currency = StripeCurrency,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["reservationId"] = hold.Id.ToString(),
                        ["projectionId"] = quote.Projection.Id.ToString(),
                        ["userId"] = userId.ToString(),
                        ["seatCount"] = quote.SeatIds.Count.ToString()
                    }
                });
            }
            catch (StripeException ex)
            {
                // No intent means nothing can ever be charged. Cancel and release the seats so the
                // row stays as an unpaid checkout that never reached Stripe, not a silent delete.
                await DiscardHoldAsync(hold);
                _logger.LogWarning(ex, "Stripe PaymentIntent creation failed for projection {ProjectionId}.", quote.Projection.Id);
                throw new ClientException(
                    ex.StripeError?.Message ?? "Could not start the payment. Please try again.");
            }

            hold.PaymentTransactionId = intent.Id;
            await _dbContext.SaveChangesAsync();

            return new PaymentIntentResponse
            {
                ReservationId = hold.Id,
                PaymentIntentId = intent.Id,
                ClientSecret = intent.ClientSecret ?? string.Empty,
                PublishableKey = _stripePublishableKey,
                TotalAmount = quote.Total,
                HoldExpiresAt = hold.HoldExpiresAt!.Value
            };
        }

        /// <summary>
        /// Frees seats of a hold that never reached Stripe. The reservation row is cancelled, not
        /// deleted, so even a failed checkout stays on record.
        /// </summary>
        private async Task DiscardHoldAsync(Reservation hold)
        {
            ReservationStatusTransitions.Apply(
                hold,
                ReservationStatus.Cancelled,
                cancellationReason: "Payment could not be started.");
            ReleaseSeats(hold);
            hold.HoldExpiresAt = null;
            await _dbContext.SaveChangesAsync();
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
                ProjectionEndTime = r.Projection?.Movie != null
                    ? r.Projection.StartTime.AddMinutes(r.Projection.Movie.DurationMinutes)
                    : default,
                PaymentMethod = (int)r.PaymentMethod,
                PaymentMethodName = r.PaymentMethod.ToString(),
                PaymentStatus = (int)r.PaymentStatus,
                PaymentStatusName = r.PaymentStatus.ToString(),
                PaymentTransactionId = r.PaymentTransactionId,
                PaymentDate = r.PaymentDate,
                RefundStatus = (int)r.RefundStatus,
                RefundStatusName = r.RefundStatus.ToString(),
                RefundId = r.RefundId,
                RefundedAt = r.RefundedAt,
                RefundError = r.RefundError,
                HoldExpiresAt = r.HoldExpiresAt,
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
