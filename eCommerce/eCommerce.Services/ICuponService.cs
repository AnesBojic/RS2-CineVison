using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface ICuponService : IBaseCRUDService<CuponResponse, CuponSearch, CuponInsertRequest, CuponUpdateRequest>
    {
        Task ToggleActivityAsync(int id);
    }
}
