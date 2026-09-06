using CineVision.Model;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class ProjectionsController : BaseCRUDController<ProjectionResponse, ProjectionSearchObject, ProjectionInsertRequest, ProjectionUpdateRequest, IProjectionService>
{
    public ProjectionsController(IProjectionService projectionService) : base(projectionService)
    {
    }

    protected override string WritePermission => RolePermissionNames.ManageProjections;

    [HttpGet("{id}/Seats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ProjectionSeatResponse>>> GetSeats(int id)
    {
        var result = await _service.GetSeatsAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Preview of related records that will be removed if this projection is cascade-deleted.
    /// </summary>
    [Authorize(Policy = RolePermissionNames.ManageProjections)]
    [HttpGet("{id}/DeleteImpact")]
    [ProducesResponseType(typeof(CascadeDeleteImpactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CascadeDeleteImpactResponse>> GetDeleteImpact(int id)
    {
        var result = await _service.GetDeleteImpactAsync(id);
        return Ok(result);
    }

    [Authorize(Policy = RolePermissionNames.ManageProjections)]
    [HttpPost("{id}/Cancel")]
    [ProducesResponseType(typeof(ProjectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectionResponse>> Cancel(int id, [FromBody] ProjectionCancelRequest? request)
    {
        var result = await _service.CancelAsync(id, request);
        return Ok(result);
    }
}
