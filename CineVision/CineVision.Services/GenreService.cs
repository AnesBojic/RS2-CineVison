using System;
using System.Collections.Generic;
using System.Linq;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services.Database;
using FluentValidation;

namespace CineVision.Services
{
    public class GenreService : BaseCRUDService<Genre, GenreResponse, GenreSearchObject, GenreInsertRequest, GenreUpdateRequest>, IGenreService
    {
        public GenreService(CineVisionDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<GenreInsertRequest> insertValidator, IValidator<GenreUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override string? DefaultSortBy => "Id desc";

        protected override IQueryable<Genre> ApplyFilters(IQueryable<Genre> query, GenreSearchObject? search)
        {
            if (search != null && !string.IsNullOrWhiteSpace(search.Name))
            {
                var name = search.Name;
                query = query.Where(g => g.Name.Contains(name));
            }

            return query;
        }
    }
}
