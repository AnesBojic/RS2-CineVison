using eCommerce.Model;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class ScreeningsController : BaseCRUDController<ScreeningResponse, ScreeningSearchObject, ScreeningInsertRequest, ScreeningUpdateRequest, IScreeningService>
{
    public ScreeningsController(IScreeningService screeningService) : base(screeningService)
    {
    }

    [Authorize]
    [HttpGet("{id}/Seats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ScreeningSeatResponse>>> GetSeats(int id)
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
