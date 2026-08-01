using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class LanguagesController : BaseCRUDController<LanguageResponse, LookupSearchObject, LanguageInsertRequest, LanguageUpdateRequest, ILanguageService>
{
    public LanguagesController(ILanguageService service) : base(service)
    {
    }
}
