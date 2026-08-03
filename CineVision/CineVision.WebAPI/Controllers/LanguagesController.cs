using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class LanguagesController : BaseCRUDController<LanguageResponse, LookupSearchObject, LanguageInsertRequest, LanguageUpdateRequest, ILanguageService>
{
    public LanguagesController(ILanguageService service) : base(service)
    {
    }
}
