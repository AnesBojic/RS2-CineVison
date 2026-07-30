using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class NewsController : BaseCRUDController<NewsResponse, NewsSearchObject, NewsInsertRequest, NewsUpdateRequest, INewsService>
{
    public NewsController(INewsService newsService) : base(newsService)
    {
    }
}
