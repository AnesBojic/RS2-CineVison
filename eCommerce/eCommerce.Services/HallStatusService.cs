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
    public class HallStatusService
        : LookupService<HallStatus, HallStatusResponse, HallStatusInsertRequest, HallStatusUpdateRequest>, IHallStatusService
    {
        public HallStatusService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<HallStatusInsertRequest> insertValidator,
            IValidator<HallStatusUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override string EntityLabel => "hall status";

        protected override async Task<Dictionary<int, int>> CountUsagesAsync(IReadOnlyCollection<int> ids)
        {
            return await _dbContext.Halls
                .Where(h => ids.Contains(h.StatusId))
                .GroupBy(h => h.StatusId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);
        }
    }
}
