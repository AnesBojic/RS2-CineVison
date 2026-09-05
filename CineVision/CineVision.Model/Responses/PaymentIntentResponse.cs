namespace CineVision.Model.Responses
{
    public class PaymentIntentResponse
    {
        /// <summary>Seat hold created for this payment; the booking that will be finalised.</summary>
        public int ReservationId { get; set; }

        /// <summary>Stripe PaymentIntent id (e.g. pi_...), for server-side confirm / idempotency.</summary>
        public string PaymentIntentId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;

        /// <summary>Server-calculated total the customer is charged; clients must not price bookings.</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>When the held seats are released if the payment is not completed.</summary>
        public DateTime HoldExpiresAt { get; set; }
    }
}
