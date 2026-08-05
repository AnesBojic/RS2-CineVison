using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;

namespace CineVision.Services
{
    public interface IProjectionService : IBaseCRUDService<ProjectionResponse, ProjectionSearchObject, ProjectionInsertRequest, ProjectionUpdateRequest>
    {
        /// <summary>
        /// Returns the full seat map of the projection's hall, flagging which seats are already taken.
        /// </summary>
        Task<List<ProjectionSeatResponse>> GetSeatsAsync(int projectionId);

        /// <summary>Preview of related rows removed by cascade delete.</summary>
        Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id);
    }
}
