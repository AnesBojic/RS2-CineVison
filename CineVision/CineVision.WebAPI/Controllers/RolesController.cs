using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;

namespace CineVision.WebAPI.Controllers;

/// <summary>
/// Reference-data CRUD for roles. Writes follow the same AdminStaff rule as other lookups.
/// Admin, Staff, and Customer names stay frozen because JWT [Authorize] depends on them.
/// </summary>
[Authorize]
public class RolesController : BaseCRUDController<RoleResponse, LookupSearchObject, RoleInsertRequest, RoleUpdateRequest, IRoleService>
{
    public RolesController(IRoleService service) : base(service)
    {
    }
}
