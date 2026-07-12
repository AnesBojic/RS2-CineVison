using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using System.Threading.Tasks;

namespace eCommerce.Services
{
    public interface IHallService : IBaseCRUDService<HallResponse, HallSearchObject, HallInsertRequest, HallUpdateRequest>
    {
        Task<HallResponse> UpdateSeatLayoutAsync(int hallId, HallSeatLayoutUpdateRequest request);
    }
}
