using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class GenresController : BaseCRUDController<GenreResponse, GenreSearchObject, GenreInsertRequest, GenreUpdateRequest, IGenreService>
{
    public GenresController(IGenreService genreService) : base(genreService)
    {
    }
}
