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
    public class AgeRatingService
        : LookupService<AgeRating, AgeRatingResponse, AgeRatingInsertRequest, AgeRatingUpdateRequest>, IAgeRatingService
    {
        public AgeRatingService(
            CineVisionDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<AgeRatingInsertRequest> insertValidator,
            IValidator<AgeRatingUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override string EntityLabel => "age rating";

        protected override async Task<Dictionary<int, int>> CountUsagesAsync(IReadOnlyCollection<int> ids)
        {
            return await _dbContext.Movies
                .Where(m => m.AgeRatingId != null && ids.Contains(m.AgeRatingId.Value))
                .GroupBy(m => m.AgeRatingId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);
        }
    }
}
