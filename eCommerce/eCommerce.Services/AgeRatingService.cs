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
    public class AgeRatingService
        : LookupService<AgeRating, AgeRatingResponse, AgeRatingInsertRequest, AgeRatingUpdateRequest>, IAgeRatingService
    {
        public AgeRatingService(
            ECommerceDbContext dbContext,
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
