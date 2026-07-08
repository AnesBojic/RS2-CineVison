using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class ScreeningsController : BaseCRUDController<ScreeningResponse, ScreeningSearchObject, ScreeningInsertRequest, ScreeningUpdateRequest, IScreeningService>
{
    public ScreeningsController(IScreeningService screeningService) : base(screeningService)
    {
    }

    [AllowAnonymous]
    public override Task<PageResult<ScreeningResponse>> GetAll([FromQuery] ScreeningSearchObject? search)
    {
        return base.GetAll(search);
    }

    [AllowAnonymous]
    public override Task<ActionResult<ScreeningResponse>> GetById(int id)
    {
        return base.GetById(id);
    }

    /// <summary>
    /// Returns the seat map for a screening, marking which seats are already taken.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id}/Seats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ScreeningSeatResponse>>> GetSeats(int id)
    {
        var result = await _service.GetSeatsAsync(id);
        return Ok(result);
    }
}
