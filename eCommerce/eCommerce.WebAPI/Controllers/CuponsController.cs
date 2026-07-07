using eCommerce.Services;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Model.Requests;

namespace eCommerce.WebAPI.Controllers;

public class CuponsController : BaseCRUDController<CuponResponse, CuponSearch, CuponInsertRequest, CuponUpdateRequest, ICuponService>
{
    public CuponsController(ICuponService cuponService) : base(cuponService)
    {
    }

    [HttpPut("{id}/ToggleActivity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> ToggleActivity(int id)
    {
        await _service.ToggleActivityAsync(id);
        return Ok();
    }
}
