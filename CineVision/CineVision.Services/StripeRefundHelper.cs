using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stripe;

namespace CineVision.Services;

internal static class StripeRefundHelper
{
    public static async Task TryRefundAsync(string? secretKey, string paymentIntentId, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                logger.LogWarning(
                    "Stripe secret key missing; cannot refund payment intent {PaymentIntentId}.",
                    paymentIntentId);
                return;
            }

            StripeConfiguration.ApiKey = secretKey;
            var refundService = new RefundService();
            await refundService.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stripe refund failed for PaymentIntent {PaymentIntentId}.", paymentIntentId);
        }
    }
}
