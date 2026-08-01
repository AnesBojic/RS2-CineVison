using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class ScreenTypeService
        : LookupService<ScreenType, ScreenTypeResponse, ScreenTypeInsertRequest, ScreenTypeUpdateRequest>, IScreenTypeService
    {
        public ScreenTypeService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ScreenTypeInsertRequest> insertValidator,
            IValidator<ScreenTypeUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override string EntityLabel => "screen type";

        protected override async Task<Dictionary<int, int>> CountUsagesAsync(IReadOnlyCollection<int> ids)
        {
            return await _dbContext.Halls
                .Where(h => ids.Contains(h.ScreenTypeId))
                .GroupBy(h => h.ScreenTypeId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);
        }
    }
}
