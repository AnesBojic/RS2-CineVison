using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IProductService : IBaseCRUDService<ProductResponse, ProductSearchObject, ProductInsertRequest, ProductUpdateRequest>
    {
        Task<ProductResponse> GetWithMaxNameAsync(ProductSearchObject? search = null);

        Task<ProductResponse> ActivateAsync(int id);
        Task<ProductResponse> DeactivateAsync(int id);

        Task<List<string>> GetAllowedActionsAsync(int id);
    }
}