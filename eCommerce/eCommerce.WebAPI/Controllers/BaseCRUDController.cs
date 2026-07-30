using Microsoft.AspNetCore.Mvc;
using eCommerce.Services;
using eCommerce.Model.SearchObjects;
using eCommerce.Model;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.WebAPI.Controllers;

/// <summary>
/// Generic base controller for CRUD operations (Create, Read, Update, Delete)
/// </summary>
[ApiController]
[Route("[controller]")]
public abstract class BaseCRUDController<TResponse, TSearch, TInsertRequest, TUpdateRequest, TService>
    : BaseReadController<TResponse, TSearch, TService>
    where TSearch : BaseSearchObject
    where TService : IBaseCRUDService<TResponse, TSearch, TInsertRequest, TUpdateRequest>
{
    protected BaseCRUDController(TService service) : base(service)
    {
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminStaff)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<TResponse>> Create([FromBody] TInsertRequest request)
    {
        var result = await _service.InsertAsync(request);
        return result;
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.AdminStaff)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<TResponse>> Update(int id, [FromBody] TUpdateRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result;
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.AdminStaff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> Delete(int id)
    {
         await _service.DeleteAsync(id);
        return NoContent();
    }
}
