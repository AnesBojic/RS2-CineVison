using eCommerce.Model.Responses;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

/// <summary>
/// Personalized hybrid movie recommendations. Exposed under /Movies so the mobile app can call
/// GET /Movies/Recommendations; requires an authenticated user (recommendations are per-user).
/// </summary>
[ApiController]
[Route("Movies")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    /// <summary>Top personalized recommendations for the current user (popularity + content based).</summary>
    [HttpGet("Recommendations")]
    [ProducesResponseType(typeof(List<RecommendationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RecommendationResponse>>> GetRecommendations([FromQuery] int take = 10)
    {
        var result = await _recommendationService.GetRecommendationsAsync(take);
        return Ok(result);
    }
}
