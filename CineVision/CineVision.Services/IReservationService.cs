using CineVision.Model.Requests;

using CineVision.Model.Responses;

using CineVision.Model.SearchObjects;



namespace CineVision.Services

{

    public interface IReservationService : IBaseReadService<ReservationResponse, ReservationSearchObject>

    {

        Task<ReservationResponse> CreateReservationAsync(ReservationCreateRequest request);



        Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);



        Task<ReservationResponse> CancelAsync(int id, ReservationCancelRequest? request = null);



        Task<ReservationResponse> CompleteAsync(int id);

    }

}

