using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class HallStatusesController : BaseCRUDController<HallStatusResponse, LookupSearchObject, HallStatusInsertRequest, HallStatusUpdateRequest, IHallStatusService>
{
    public HallStatusesController(IHallStatusService service) : base(service)
    {
    }
}
