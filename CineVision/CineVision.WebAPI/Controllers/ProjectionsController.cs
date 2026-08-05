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

    /// <summary>Public showtimes for guest browsing.</summary>
    [AllowAnonymous]
    [HttpGet]
    public override Task<PageResult<ProjectionResponse>> GetAll([FromQuery] ProjectionSearchObject? search)
        => base.GetAll(search);

    [AllowAnonymous]
    [HttpGet("{id}")]
    public override Task<ActionResult<ProjectionResponse>> GetById(int id)
        => base.GetById(id);

    /// <summary>Public seat map so guests can pick seats before signing in at checkout.</summary>
    [AllowAnonymous]
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
    [Authorize(Roles = RoleNames.AdminStaff)]
    [HttpGet("{id}/DeleteImpact")]
    [ProducesResponseType(typeof(CascadeDeleteImpactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CascadeDeleteImpactResponse>> GetDeleteImpact(int id)
    {
        var result = await _service.GetDeleteImpactAsync(id);
        return Ok(result);
    }
}
