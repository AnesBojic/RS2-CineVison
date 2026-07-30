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
    public class NewsService : BaseCRUDService<News, NewsResponse, NewsSearchObject, NewsInsertRequest, NewsUpdateRequest>, INewsService
    {
        public NewsService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<NewsInsertRequest> insertValidator,
            IValidator<NewsUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IEnumerable<News> ApplyFilters(IEnumerable<News> query, NewsSearchObject? search)
        {
            if (search == null)
            {
                return query;
            }

            if (!string.IsNullOrWhiteSpace(search.Title))
            {
                query = query.Where(n => n.Title.Contains(search.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (search.IsActive.HasValue)
            {
                query = query.Where(n => n.IsActive == search.IsActive.Value);
            }

            return query.OrderByDescending(n => n.PublishedAt);
        }

        protected override News MapInsertRequestToEntity(NewsInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.PublishedAt = request.PublishedAt ?? DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;
            return entity;
        }

        protected override void MapUpdateRequestToEntity(NewsUpdateRequest request, News entity)
        {
            base.MapUpdateRequestToEntity(request, entity);
            if (request.PublishedAt.HasValue)
            {
                entity.PublishedAt = request.PublishedAt.Value;
            }

            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
