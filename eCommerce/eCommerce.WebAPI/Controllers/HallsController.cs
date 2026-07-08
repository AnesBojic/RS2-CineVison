using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.WebAPI.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class HallsController : BaseCRUDController<HallResponse, HallSearchObject, HallInsertRequest, HallUpdateRequest, IHallService>
{
    public HallsController(IHallService hallService) : base(hallService)
    {
    }
}
