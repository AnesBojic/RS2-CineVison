using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class NewsController : BaseCRUDController<NewsResponse, NewsSearchObject, NewsInsertRequest, NewsUpdateRequest, INewsService>
{
    public NewsController(INewsService newsService) : base(newsService)
    {
    }
}
