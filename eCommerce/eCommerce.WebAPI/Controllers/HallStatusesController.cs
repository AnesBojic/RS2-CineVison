using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class HallStatusesController : BaseCRUDController<HallStatusResponse, LookupSearchObject, HallStatusInsertRequest, HallStatusUpdateRequest, IHallStatusService>
{
    public HallStatusesController(IHallStatusService service) : base(service)
    {
    }
}
