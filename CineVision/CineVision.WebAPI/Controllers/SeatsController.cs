using CineVision.Model;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;

namespace CineVision.WebAPI.Controllers;

[Authorize(Roles = RoleNames.AdminStaff)]
public class SeatsController : BaseCRUDController<SeatResponse, SeatSearchObject, SeatInsertRequest, SeatUpdateRequest, ISeatService>
{
    public SeatsController(ISeatService seatService) : base(seatService)
    {
    }
}
