using CineVision.Model;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class ReservationsController : BaseReadController<ReservationResponse, ReservationSearchObject, IReservationService>
{
    public ReservationsController(IReservationService reservationService)
        : base(reservationService)
    {
    }

    /// <summary>
    /// Reserves seats for a screening. When paymentIntentId is set, Stripe is verified server-side before Paid.
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

    /// <summary>
    /// Customer cancels own booking (4h rule). Admin or Staff may cancel any booking with a reason.
    /// </summary>
    [HttpPost("{id}/Cancel")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Cancel(int id, [FromBody] ReservationCancelRequest? request)
    {
        var result = await _service.CancelAsync(id, request);
        return Ok(result);
    }

    /// <summary>Marks a Confirmed or Paid booking as Completed (Admin or Staff).</summary>
    [HttpPost("{id}/Complete")]
    [Authorize(Roles = RoleNames.AdminStaff)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationResponse>> Complete(int id)
    {
        var result = await _service.CompleteAsync(id);
        return Ok(result);
    }
}
