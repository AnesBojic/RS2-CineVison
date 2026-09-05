namespace CineVision.Model.Enums
{
    /// <summary>
    /// How a booking is paid for. A booking without an online payment is only valid when it is
    /// explicitly registered as a counter sale, never as a missing Stripe payment.
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>Stripe checkout: seats are held first, the booking is finalised after the PaymentIntent succeeds.</summary>
        Online = 1,

        /// <summary>Box-office sale registered by Admin or Staff; the customer pays at the cinema.</summary>
        Counter = 2
    }
}
