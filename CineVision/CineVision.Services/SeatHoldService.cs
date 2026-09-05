using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CineVision.Model.Enums;
using CineVision.Services.Database;
using CineVision.Services.ReservationStateMachine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace CineVision.Services
{
    /// <summary>
    /// Releases seat holds (Pending reservations) that were created before payment but never
    /// finalised. A hold whose PaymentIntent actually succeeded is finalised instead of released:
    /// the customer's money left their account, so the seats must stay theirs.
    /// </summary>
    public class SeatHoldService : ISeatHoldService
    {
        private const string StripeSucceededStatus = "succeeded";

        private readonly CineVisionDbContext _dbContext;
        private readonly string? _stripeSecretKey;
        private readonly ILogger<SeatHoldService> _logger;

        public SeatHoldService(
            CineVisionDbContext dbContext,
            IConfiguration configuration,
            ILogger<SeatHoldService> logger)
        {
            _dbContext = dbContext;
            _stripeSecretKey = configuration["Stripe:SecretKey"];
            _logger = logger;
        }

        public Task ReleaseExpiredHoldsAsync(int projectionId)
        {
            var now = DateTime.UtcNow;
            return ReleaseAsync(
                _dbContext.Reservations
                    .Where(r => r.ProjectionId == projectionId
                                && r.Status == ReservationStatus.Pending
                                && r.HoldExpiresAt != null
                                && r.HoldExpiresAt < now),
                "Seat hold expired before the payment was completed.");
        }

        public Task ReleaseOwnHoldsAsync(int userId, int projectionId)
        {
            return ReleaseAsync(
                _dbContext.Reservations
                    .Where(r => r.ProjectionId == projectionId
                                && r.UserId == userId
                                && r.Status == ReservationStatus.Pending
                                && r.HoldExpiresAt != null),
                "Checkout was restarted for the same projection.");
        }

        private async Task ReleaseAsync(IQueryable<Reservation> holds, string reason)
        {
            var candidates = await holds
                .Include(r => r.ReservationSeats)
                .ToListAsync();

            if (candidates.Count == 0)
            {
                return;
            }

            var changed = false;

            foreach (var hold in candidates)
            {
                var paid = await WasAlreadyPaidAsync(hold);

                if (paid == true)
                {
                    // Money cleared while the hold was expiring; honour the booking instead of
                    // freeing seats the customer has already paid for.
                    ReservationStatusTransitions.Apply(hold, ReservationStatus.Paid);
                    hold.HoldExpiresAt = null;
                    changed = true;

                    _logger.LogWarning(
                        "Seat hold {ReservationId} was paid but never finalised by the client; marked as Paid.",
                        hold.Id);
                    continue;
                }

                if (paid == null)
                {
                    // Stripe is unreachable: leave the hold alone rather than risk releasing seats
                    // that were paid for. The next call retries.
                    continue;
                }

                await TryCancelPaymentIntentAsync(hold.PaymentTransactionId);

                ReservationStatusTransitions.Apply(
                    hold,
                    ReservationStatus.Cancelled,
                    cancellationReason: reason);

                // An unpaid hold never carried money, so its seat rows are freed outright and the
                // cancelled reservation stays as the audit trail.
                _dbContext.ReservationSeats.RemoveRange(hold.ReservationSeats);
                hold.HoldExpiresAt = null;
                changed = true;
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// True when Stripe confirms the money cleared, false when it clearly did not, and null
        /// when the payment state could not be read at all.
        /// </summary>
        private async Task<bool?> WasAlreadyPaidAsync(Reservation hold)
        {
            if (string.IsNullOrWhiteSpace(hold.PaymentTransactionId))
            {
                // The hold never reached Stripe, so no money can be involved.
                return false;
            }

            if (string.IsNullOrWhiteSpace(_stripeSecretKey))
            {
                return null;
            }

            try
            {
                StripeConfiguration.ApiKey = _stripeSecretKey;
                var intent = await new PaymentIntentService().GetAsync(hold.PaymentTransactionId);
                return string.Equals(intent.Status, StripeSucceededStatus, StringComparison.OrdinalIgnoreCase);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not read PaymentIntent {PaymentIntentId} while releasing hold {ReservationId}.",
                    hold.PaymentTransactionId,
                    hold.Id);
                return null;
            }
        }

        private async Task TryCancelPaymentIntentAsync(string? paymentIntentId)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId) || string.IsNullOrWhiteSpace(_stripeSecretKey))
            {
                return;
            }

            try
            {
                StripeConfiguration.ApiKey = _stripeSecretKey;
                await new PaymentIntentService().CancelAsync(paymentIntentId);
            }
            catch (StripeException ex)
            {
                // Already cancelled or in a state Stripe will not cancel; the hold is released anyway.
                _logger.LogInformation(
                    ex,
                    "PaymentIntent {PaymentIntentId} could not be cancelled while releasing its hold.",
                    paymentIntentId);
            }
        }
    }
}
