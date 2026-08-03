using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace CineVision.Services
{
    public abstract class BaseReadService<TEntity, TResponse, TSearch> : IBaseReadService<TResponse, TSearch>
        where TEntity : class
        where TSearch : BaseSearchObject, new()
        where TResponse : class
    {
        protected readonly MapsterMapper.IMapper _mapper;
        protected readonly CineVisionDbContext _dbContext;

        protected BaseReadService(MapsterMapper.IMapper mapper, CineVisionDbContext dbContext)
        {
            _mapper = mapper;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Applies search filters against an <see cref="IQueryable{T}"/> so EF can push them to SQL.
        /// Override in derived classes â€” do not materialize the query here.
        /// </summary>
        protected abstract IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query, TSearch? search);

        /// <summary>
        /// Ordering applied when the caller does not ask for one. Admin-managed lists set this
        /// to newest first so a row saved a moment ago is on top rather than on the last page.
        /// </summary>
        protected virtual string? DefaultSortBy => null;

        public virtual async Task<PageResult<TResponse>> GetAllAsync(TSearch? search = null)
        {
            search ??= new TSearch();
            PagingLimits.Normalize(search);

            IQueryable<TEntity> query = _dbContext.Set<TEntity>().AsNoTracking();
            query = await IncludeRelatedEntitiesAsync(search, query);
            query = ApplyFilters(query, search);

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = await query.CountAsync();
            }

            var sortBy = string.IsNullOrWhiteSpace(search.SortBy) ? DefaultSortBy : search.SortBy;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                // SortBy is an allow-listed Dynamic LINQ expression from trusted admin clients.
                query = query.OrderBy(sortBy);
            }

            // Filter â†’ count â†’ sort â†’ page, then materialize. Loading the whole table first is
            // exactly what the performance rules forbid.
            query = query
                .Skip((search.Page!.Value - 1) * search.PageSize!.Value)
                .Take(search.PageSize.Value);

            var entities = await query.ToListAsync();
            var list = entities.Select(item => _mapper.Map<TResponse>(item)).ToList();

            return new PageResult<TResponse>
            {
                Items = list,
                TotalCount = totalCount
            };
        }

        protected virtual Task<IQueryable<TEntity>> IncludeRelatedEntitiesAsync(TSearch? search, IQueryable<TEntity> query)
        {
            return Task.FromResult(query);
        }

        public virtual async Task<TResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<TEntity>().FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"{typeof(TEntity).Name} with id {id} not found.");
            }

            return _mapper.Map<TResponse>(entity);
        }
    }
}
