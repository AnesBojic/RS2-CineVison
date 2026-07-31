using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IScreeningService : IBaseCRUDService<ScreeningResponse, ScreeningSearchObject, ScreeningInsertRequest, ScreeningUpdateRequest>
    {
        /// <summary>
        /// Returns the full seat map of the screening's hall, flagging which seats are already taken.
        /// </summary>
        Task<List<ScreeningSeatResponse>> GetSeatsAsync(int screeningId);

        /// <summary>Preview of related rows removed by cascade delete.</summary>
        Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id);
    }
}
