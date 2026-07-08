using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;

namespace eCommerce.Services
{
    public class GenreService : BaseCRUDService<Genre, GenreResponse, GenreSearchObject, GenreInsertRequest, GenreUpdateRequest>, IGenreService
    {
        public GenreService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<GenreInsertRequest> insertValidator, IValidator<GenreUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IEnumerable<Genre> ApplyFilters(IEnumerable<Genre> query, GenreSearchObject? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    query = query.Where(g => g.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
                }
            }

            return query;
        }
    }
}
