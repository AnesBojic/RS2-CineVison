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
    public class LanguageService
        : LookupService<Language, LanguageResponse, LanguageInsertRequest, LanguageUpdateRequest>, ILanguageService
    {
        public LanguageService(
            CineVisionDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<LanguageInsertRequest> insertValidator,
            IValidator<LanguageUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override string EntityLabel => "language";

        /// <summary>A language can be referenced both by movies and by individual projections.</summary>
        protected override async Task<Dictionary<int, int>> CountUsagesAsync(IReadOnlyCollection<int> ids)
        {
            var movieCounts = await _dbContext.Movies
                .Where(m => m.LanguageId != null && ids.Contains(m.LanguageId.Value))
                .GroupBy(m => m.LanguageId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            var screeningCounts = await _dbContext.Screenings
                .Where(s => s.LanguageId != null && ids.Contains(s.LanguageId.Value))
                .GroupBy(s => s.LanguageId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            return movieCounts.Concat(screeningCounts)
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
        }
    }
}
