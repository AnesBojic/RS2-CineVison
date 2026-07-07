using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eCommerce.Services
{
    public interface IBaseReadService<TResponse, TSearch>
        where TSearch : BaseSearchObject
    {
        Task<TResponse> GetByIdAsync(int id);
        Task<PageResult<TResponse>> GetAllAsync(TSearch? search = null);
    }
}
