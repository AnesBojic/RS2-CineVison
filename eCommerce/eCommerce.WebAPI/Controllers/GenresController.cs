using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class GenresController : BaseCRUDController<GenreResponse, GenreSearchObject, GenreInsertRequest, GenreUpdateRequest, IGenreService>
{
    public GenresController(IGenreService genreService) : base(genreService)
    {
    }

    [AllowAnonymous]
    public override Task<PageResult<GenreResponse>> GetAll([FromQuery] GenreSearchObject? search)
    {
        return base.GetAll(search);
    }

    [AllowAnonymous]
    public override Task<ActionResult<GenreResponse>> GetById(int id)
    {
        return base.GetById(id);
    }
}
