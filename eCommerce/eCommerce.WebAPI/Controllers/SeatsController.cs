using eCommerce.Model;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.WebAPI.Controllers;

[Authorize(Roles = RoleNames.AdminStaff)]
public class SeatsController : BaseCRUDController<SeatResponse, SeatSearchObject, SeatInsertRequest, SeatUpdateRequest, ISeatService>
{
    public SeatsController(ISeatService seatService) : base(seatService)
    {
    }
}
