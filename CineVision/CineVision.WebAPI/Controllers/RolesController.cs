using CineVision.Model;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;

namespace CineVision.WebAPI.Controllers;

/// <summary>
/// Reference-data CRUD for roles. Writes require ManageRoles.
/// Admin and Customer names stay frozen; Staff is optional. Permissions (except Admin) and color are editable.
/// </summary>
[Authorize]
public class RolesController : BaseCRUDController<RoleResponse, LookupSearchObject, RoleInsertRequest, RoleUpdateRequest, IRoleService>
{
    public RolesController(IRoleService service) : base(service)
    {
    }

    protected override string WritePermission => RolePermissionNames.ManageRoles;
}
