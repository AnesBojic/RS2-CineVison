using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface INewsService : IBaseCRUDService<NewsResponse, NewsSearchObject, NewsInsertRequest, NewsUpdateRequest>
    {
    }
}
