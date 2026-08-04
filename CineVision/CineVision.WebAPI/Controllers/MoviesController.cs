using CineVision.Model;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class MoviesController : BaseCRUDController<MovieResponse, MovieSearchObject, MovieInsertRequest, MovieUpdateRequest, IMovieService>
{
    public MoviesController(IMovieService movieService) : base(movieService)
    {
    }

    /// <summary>Public catalog listing for the mobile app (guests can browse).</summary>
    [AllowAnonymous]
    [HttpGet]
    public override Task<PageResult<MovieResponse>> GetAll([FromQuery] MovieSearchObject? search)
        => base.GetAll(search);

    /// <summary>Public movie details for browsing before login.</summary>
    [AllowAnonymous]
    [HttpGet("{id}")]
    public override Task<ActionResult<MovieResponse>> GetById(int id)
        => base.GetById(id);

    [Authorize]
    [HttpPost("SearchHistory")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordSearch([FromBody] RecordSearchRequest request)
    {
        await _service.RecordSearchAsync(request);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{id}/View")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterView(int id)
    {
        await _service.RegisterViewAsync(id);
        return NoContent();
    }

    [Authorize(Roles = RoleNames.AdminStaff)]
    [HttpPut("{id}/Poster")]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieResponse>> UpdatePoster(int id, [FromBody] MoviePosterUpdateRequest request)
    {
        var result = await _service.UpdatePosterAsync(id, request);
        return Ok(result);
    }

    [Authorize(Roles = RoleNames.AdminStaff)]
    [HttpPost("{id}/Activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieResponse>> Activate(int id)
    {
        var result = await _service.ActivateAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = RoleNames.AdminStaff)]
    [HttpPost("{id}/Deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieResponse>> Deactivate(int id)
    {
        var result = await _service.DeactivateAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = RoleNames.AdminStaff)]
    [HttpGet("{id}/AllowedActions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<string>>> GetAllowedActions(int id)
    {
        var result = await _service.GetAllowedActionsAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Preview of related records that will be removed if this movie is cascade-deleted.
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
