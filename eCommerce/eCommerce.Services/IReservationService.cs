using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IReservationService : IBaseReadService<ReservationResponse, ReservationSearchObject>
    {
        Task<ReservationResponse> CreateReservationAsync(ReservationCreateRequest request);

        Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);

        Task<ReservationResponse> CancelAsync(int id);
    }
}
