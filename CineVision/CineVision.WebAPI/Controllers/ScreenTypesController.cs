using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class ScreenTypesController : BaseCRUDController<ScreenTypeResponse, LookupSearchObject, ScreenTypeInsertRequest, ScreenTypeUpdateRequest, IScreenTypeService>
{
    public ScreenTypesController(IScreenTypeService service) : base(service)
    {
    }
}
