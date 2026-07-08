using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class MoviesController : BaseCRUDController<MovieResponse, MovieSearchObject, MovieInsertRequest, MovieUpdateRequest, IMovieService>
{
    public MoviesController(IMovieService movieService) : base(movieService)
    {
    }

    [AllowAnonymous]
    public override Task<PageResult<MovieResponse>> GetAll([FromQuery] MovieSearchObject? search)
    {
        return base.GetAll(search);
    }

    [AllowAnonymous]
    public override Task<ActionResult<MovieResponse>> GetById(int id)
    {
        return base.GetById(id);
    }

    [AllowAnonymous]
    [HttpPost("{id}/View")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterView(int id)
    {
        await _service.RegisterViewAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Upload or replace the movie poster (base64 image). Matches the desktop "upload poster" action.
    /// </summary>
    [HttpPut("{id}/Poster")]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieResponse>> UpdatePoster(int id, [FromBody] MoviePosterUpdateRequest request)
    {
        var result = await _service.UpdatePosterAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id}/Activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieResponse>> Activate(int id)
    {
        var result = await _service.ActivateAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/Deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieResponse>> Deactivate(int id)
    {
        var result = await _service.DeactivateAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}/AllowedActions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<string>>> GetAllowedActions(int id)
    {
        var result = await _service.GetAllowedActionsAsync(id);
        return Ok(result);
    }
}
