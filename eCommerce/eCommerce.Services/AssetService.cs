using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using MapsterMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.Services
{
    public class AssetService : BaseCRUDService<Asset, AssetResponse, AssetSearch, AssetInsertRequest, AssetUpdateRequest>, IAssetService
    {
        public AssetService(ECommerceDbContext dbContext, IMapper mapper, IValidator<AssetInsertRequest> insertValidator, IValidator<AssetUpdateRequest> updateValidator)
           : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IQueryable<Asset> ApplyFilters(IQueryable<Asset> query, AssetSearch? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.FileName))
                {
                    var fileName = search.FileName;
                    query = query.Where(a => a.FileName.Contains(fileName));
                }

                if (!string.IsNullOrWhiteSpace(search.ContentType))
                {
                    var contentType = search.ContentType;
                    query = query.Where(a => a.ContentType.Contains(contentType));
                }

                if (search.MovieId.HasValue)
                {
                    query = query.Where(a => a.MovieId == search.MovieId.Value);
                }
            }

            return query;
        }
    }
}
