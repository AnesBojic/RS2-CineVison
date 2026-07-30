namespace eCommerce.Model.Responses
{
    public class PaymentIntentResponse
    {
        /// <summary>Stripe PaymentIntent id (e.g. pi_...), for server-side confirm / idempotency.</summary>
        public string PaymentIntentId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
    }
}
