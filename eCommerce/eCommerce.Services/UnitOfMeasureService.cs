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
    public class UnitOfMeasureService : BaseCRUDService<UnitOfMeasure, UnitOfMeasureResponse, UnitOfMeasureSearch, UnitOfMeasureInsertRequest, UnitOfMeasureUpdateRequest>, IUnitOfMeasureService
    {
        public UnitOfMeasureService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<UnitOfMeasureInsertRequest> insertValidator, IValidator<UnitOfMeasureUpdateRequest> updateValidator) : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IEnumerable<UnitOfMeasure> ApplyFilters(IEnumerable<UnitOfMeasure> query, UnitOfMeasureSearch? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    query = query.Where(u => u.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(search.Abbreviation))
                {
                    query = query.Where(u => u.Abbreviation.Contains(search.Abbreviation, StringComparison.OrdinalIgnoreCase));
                }

                if (search.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == search.IsActive.Value);
                }
            }

            return query;
        }
    }
}
