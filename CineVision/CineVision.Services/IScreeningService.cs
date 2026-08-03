using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;

namespace CineVision.Services
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
