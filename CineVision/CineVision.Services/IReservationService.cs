using System.Collections.Generic;
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

        /// <summary>
        /// Cancels every still-active booking on a projection (staff path: no 4-hour customer window).
        /// Refunds are recorded the same way as a customer cancel: Pending first, then Stripe.
        /// </summary>
        Task<IReadOnlyList<ReservationResponse>> CancelActiveForProjectionAsync(
            int projectionId,
            string reason,
            int cancelledByUserId);
    }
}
