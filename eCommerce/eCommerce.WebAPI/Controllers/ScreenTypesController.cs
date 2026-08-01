using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class ScreenTypesController : BaseCRUDController<ScreenTypeResponse, LookupSearchObject, ScreenTypeInsertRequest, ScreenTypeUpdateRequest, IScreenTypeService>
{
    public ScreenTypesController(IScreenTypeService service) : base(service)
    {
    }
}
