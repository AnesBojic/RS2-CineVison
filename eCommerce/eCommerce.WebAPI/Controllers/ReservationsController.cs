using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class ReservationsController : BaseReadController<ReservationResponse, ReservationSearchObject, IReservationService>
{
    public ReservationsController(IReservationService reservationService)
        : base(reservationService)
    {
    }

    /// <summary>
    /// Reserves the selected seats for a screening. Fails if any seat is already taken.
    /// </summary>
    [HttpPost("Reserve")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReservationResponse>> Reserve([FromBody] ReservationCreateRequest request)
    {
        var result = await _service.CreateReservationAsync(request);
        return Ok(result);
    }

    [HttpPost("CreatePaymentIntent")]
    public async Task<ActionResult<PaymentIntentResponse>> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        var result = await _service.CreatePaymentIntentAsync(request);
        return Ok(result);
    }

    [HttpPost("{id}/Cancel")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Cancel(int id)
    {
        var result = await _service.CancelAsync(id);
        return Ok(result);
    }
}
