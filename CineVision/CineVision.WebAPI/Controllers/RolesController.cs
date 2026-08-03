using CineVision.Model;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;

namespace CineVision.WebAPI.Controllers;

/// <summary>
/// Feeds the role picker in the admin user form. Only administrators assign roles,
/// so the whole controller is restricted to them.
/// </summary>
[Authorize(Roles = RoleNames.Admin)]
public class RolesController : BaseReadController<RoleResponse, LookupSearchObject, IRoleService>
{
    public RolesController(IRoleService service) : base(service)
    {
    }
}
