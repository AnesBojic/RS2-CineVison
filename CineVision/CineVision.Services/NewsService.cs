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
    public class NewsService : BaseCRUDService<News, NewsResponse, NewsSearchObject, NewsInsertRequest, NewsUpdateRequest>, INewsService
    {
        public NewsService(
            CineVisionDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<NewsInsertRequest> insertValidator,
            IValidator<NewsUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        /// <summary>Newest publication first; Id breaks ties so a just-saved row lands on top.</summary>
        protected override string? DefaultSortBy => "PublishedAt desc, Id desc";

        protected override IQueryable<News> ApplyFilters(IQueryable<News> query, NewsSearchObject? search)
        {
            if (search == null)
            {
                return query;
            }

            if (!string.IsNullOrWhiteSpace(search.Title))
            {
                var title = search.Title;
                query = query.Where(n => n.Title.Contains(title));
            }

            if (search.IsActive.HasValue)
            {
                query = query.Where(n => n.IsActive == search.IsActive.Value);
            }

            return query;
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
