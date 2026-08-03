using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CineVision.Services
{
    public class ScreenTypeService
        : LookupService<ScreenType, ScreenTypeResponse, ScreenTypeInsertRequest, ScreenTypeUpdateRequest>, IScreenTypeService
    {
        public ScreenTypeService(
            CineVisionDbContext dbContext,
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
