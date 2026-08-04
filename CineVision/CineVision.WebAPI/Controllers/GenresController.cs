using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class GenresController : BaseCRUDController<GenreResponse, GenreSearchObject, GenreInsertRequest, GenreUpdateRequest, IGenreService>
{
    public GenresController(IGenreService genreService) : base(genreService)
    {
    }

    /// <summary>Public genre list for guest movie filters.</summary>
    [AllowAnonymous]
    [HttpGet]
    public override Task<PageResult<GenreResponse>> GetAll([FromQuery] GenreSearchObject? search)
        => base.GetAll(search);

    [AllowAnonymous]
    [HttpGet("{id}")]
    public override Task<ActionResult<GenreResponse>> GetById(int id)
        => base.GetById(id);
}
