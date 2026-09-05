namespace CineVision.Model.Enums
{
    /// <summary>
    /// Outcome of refunding a paid booking. Written to the database before Stripe is called, so a
    /// refund that fails or never completes stays visible instead of disappearing with the booking.
    /// </summary>
    public enum RefundStatus
    {
        /// <summary>Nothing to refund.</summary>
        None = 0,

        /// <summary>The booking was cancelled and a refund is owed but not confirmed yet.</summary>
        Pending = 1,

        /// <summary>Stripe confirmed the refund.</summary>
        Refunded = 2,

        /// <summary>Stripe rejected the refund; see the stored error and retry.</summary>
        Failed = 3
    }
}
