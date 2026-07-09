using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

/// <summary>
/// Customer movie reviews. Reads are public so movie pages can show ratings; writing a review
/// requires authentication and ownership is enforced in the service.
/// </summary>
[Authorize]
public class ReviewsController : BaseReadController<ReviewResponse, ReviewSearchObject, IReviewService>
{
    public ReviewsController(IReviewService reviewService) : base(reviewService)
    {
    }

    [AllowAnonymous]
    public override Task<PageResult<ReviewResponse>> GetAll([FromQuery] ReviewSearchObject? search)
    {
        return base.GetAll(search);
    }

    [AllowAnonymous]
    public override Task<ActionResult<ReviewResponse>> GetById(int id)
    {
        return base.GetById(id);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewResponse>> Create([FromBody] ReviewInsertRequest request)
    {
        var result = await _service.InsertAsync(request);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewResponse>> Update(int id, [FromBody] ReviewUpdateRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
