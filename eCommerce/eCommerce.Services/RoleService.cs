using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;

namespace eCommerce.Services
{
    public class RoleService : BaseReadService<Role, RoleResponse, LookupSearchObject>, IRoleService
    {
        public RoleService(MapsterMapper.IMapper mapper, ECommerceDbContext dbContext)
            : base(mapper, dbContext)
        {
        }

        /// <summary>Alphabetical: the set is fixed, so a stable order beats newest first here.</summary>
        protected override string? DefaultSortBy => "Name";

        protected override IQueryable<Role> ApplyFilters(IQueryable<Role> query, LookupSearchObject? search)
        {
            if (search == null)
            {
                return query;
            }

            if (!string.IsNullOrWhiteSpace(search.Name))
            {
                var name = search.Name;
                query = query.Where(r => r.Name.Contains(name));
            }

            if (search.IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == search.IsActive.Value);
            }

            return query;
        }
    }
}
