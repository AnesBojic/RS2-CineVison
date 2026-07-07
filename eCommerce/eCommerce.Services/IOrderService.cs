using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services;

public interface IOrderService : IBaseReadService<OrderResponse, OrderSearchObject>
{
    Task<OrderResponse> CheckoutAsync(CheckoutRequest request);

    Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);
}
