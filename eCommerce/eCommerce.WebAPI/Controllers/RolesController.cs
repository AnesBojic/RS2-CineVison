using eCommerce.Model;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.WebAPI.Controllers;

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
