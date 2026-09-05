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

        /// <summary>
        /// Takes a scheduled projection off the board: refunds active bookings, notifies customers,
        /// and keeps the row so sold tickets still describe the original show.
        /// </summary>
        Task<ProjectionResponse> CancelAsync(int id, ProjectionCancelRequest? request = null);
    }
}
