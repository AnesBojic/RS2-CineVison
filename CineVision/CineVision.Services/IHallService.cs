using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using System.Threading.Tasks;

namespace CineVision.Services
{
    public interface IHallService : IBaseCRUDService<HallResponse, HallSearchObject, HallInsertRequest, HallUpdateRequest>
    {
        Task<HallResponse> UpdateSeatLayoutAsync(int hallId, HallSeatLayoutUpdateRequest request);

        /// <summary>Preview of related rows removed by cascade delete.</summary>
        Task<CascadeDeleteImpactResponse> GetDeleteImpactAsync(int id);
    }
}
