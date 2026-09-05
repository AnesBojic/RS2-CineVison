namespace CineVision.Model.Enums
{
    /// <summary>
    /// Permanent record of whether money was actually collected for a booking. Kept independent
    /// of <see cref="ReservationStatus"/> so cancelling or completing a booking never erases the
    /// fact that it was paid.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>No payment was ever collected through the system.</summary>
        None = 0,

        /// <summary>Money was collected: Stripe confirmed the PaymentIntent, or staff took payment at the counter.</summary>
        Paid = 1
    }
}
