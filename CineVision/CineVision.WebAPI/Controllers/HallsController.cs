using CineVision.Model;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVision.WebAPI.Controllers;

[Authorize(Roles = RoleNames.AdminStaff)]
public class HallsController : BaseCRUDController<HallResponse, HallSearchObject, HallInsertRequest, HallUpdateRequest, IHallService>
{
    private readonly IHallService _hallService;

    public HallsController(IHallService hallService) : base(hallService)
    {
        _hallService = hallService;
    }

    [Authorize(Roles = RoleNames.AdminStaff)]
    [HttpPut("{id}/SeatLayout")]
    public async Task<HallResponse> UpdateSeatLayout(int id, [FromBody] HallSeatLayoutUpdateRequest request)
    {
        return await _hallService.UpdateSeatLayoutAsync(id, request);
    }

    /// <summary>
    /// Preview of related records that will be removed if this hall is cascade-deleted.
    /// </summary>
    [HttpGet("{id}/DeleteImpact")]
    [ProducesResponseType(typeof(CascadeDeleteImpactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CascadeDeleteImpactResponse>> GetDeleteImpact(int id)
    {
        var result = await _hallService.GetDeleteImpactAsync(id);
        return Ok(result);
    }
}
