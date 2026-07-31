using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace eCommerce.Services
{
    public abstract class BaseReadService<TEntity, TResponse, TSearch> : IBaseReadService<TResponse, TSearch>
        where TEntity : class
        where TSearch : BaseSearchObject, new()
        where TResponse : class
    {
        protected readonly MapsterMapper.IMapper _mapper;
        protected readonly ECommerceDbContext _dbContext;

        protected BaseReadService(MapsterMapper.IMapper mapper, ECommerceDbContext dbContext)
        {
            _mapper = mapper;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Applies search filters to the query. Override in derived classes to implement specific filtering logic.
        /// </summary>
        protected abstract IEnumerable<TEntity> ApplyFilters(IEnumerable<TEntity> query, TSearch? search);

        public virtual async Task<PageResult<TResponse>> GetAllAsync(TSearch? search = null)
        {
            search ??= new TSearch();
            PagingLimits.Normalize(search);

            IQueryable<TEntity> dbQuery = _dbContext.Set<TEntity>().AsNoTracking();
            dbQuery = await IncludeRelatedEntitiesAsync(search, dbQuery);

            // Filters stay IEnumerable for existing overrides; materialize once then page in memory.
            IEnumerable<TEntity> query = await dbQuery.ToListAsync();
            query = ApplyFilters(query, search);

            int? totalCount = null;
            if (search.IncludeTotalCount ?? false)
            {
                totalCount = query.Count();
            }

            if (!string.IsNullOrWhiteSpace(search.SortBy))
            {
                // SortBy is an allow-listed Dynamic LINQ expression from trusted admin clients.
                query = query.AsQueryable().OrderBy(search.SortBy);
            }

            query = query
                .Skip((search.Page!.Value - 1) * search.PageSize!.Value)
                .Take(search.PageSize.Value);

            var list = query.Select(item => _mapper.Map<TResponse>(item)).ToList();

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
