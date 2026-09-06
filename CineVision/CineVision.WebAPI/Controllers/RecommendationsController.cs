using CineVision.Model.Responses;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVision.WebAPI.Controllers;

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

    /// <summary>
    /// Personalized scores over the whole movie catalog (there is no Movies.IsActive flag).
    /// <paramref name="take"/> ≤ 0 is treated as 10 by the service.
    /// </summary>
    [HttpGet("Recommendations")]
    [ProducesResponseType(typeof(List<RecommendationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RecommendationResponse>>> GetRecommendations([FromQuery] int take = 0)
    {
        var result = await _recommendationService.GetRecommendationsAsync(take);
        return Ok(result);
    }
}
