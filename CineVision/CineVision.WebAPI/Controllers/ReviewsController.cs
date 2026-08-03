using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVision.WebAPI.Controllers;

/// <summary>Reviews: public reads; writes require auth and ownership checks in the service.</summary>
[Authorize]
public class ReviewsController : BaseReadController<ReviewResponse, ReviewSearchObject, IReviewService>
{
    public ReviewsController(IReviewService reviewService) : base(reviewService)
    {
    }

    [AllowAnonymous]
    [HttpGet]
    public override async Task<PageResult<ReviewResponse>> GetAll([FromQuery] ReviewSearchObject? search)
    {
        return await _service.GetAllAsync(search);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public override async Task<ActionResult<ReviewResponse>> GetById(int id)
    {
        try
        {
            return Ok(await _service.GetByIdAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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

    [HttpGet("MyEligibility")]
    [ProducesResponseType(typeof(List<ReviewEligibilityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReviewEligibilityResponse>>> GetMyEligibility()
    {
        var result = await _service.GetMyEligibilityAsync();
        return Ok(result);
    }
}
