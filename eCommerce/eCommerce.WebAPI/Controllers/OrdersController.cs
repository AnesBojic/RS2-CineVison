using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class OrdersController : BaseReadController<OrderResponse, OrderSearchObject, IOrderService>
{
    public OrdersController(IOrderService orderService)
        : base(orderService)
    {
    }

    [HttpPost("Checkout")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponse>> Checkout([FromBody] CheckoutRequest request)
    {
        var result = await _service.CheckoutAsync(request);
        return Ok(result);
    }

    [HttpPost("CreatePaymentIntent")]
    public async Task<ActionResult<PaymentIntentResponse>> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        var result = await _service.CreatePaymentIntentAsync(request);
        return Ok(result);
    }
}
//owasp top 10.