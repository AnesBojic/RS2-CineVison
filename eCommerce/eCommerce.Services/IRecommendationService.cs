using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface IRecommendationService
    {
        /// <summary>Ranked active movies; <paramref name="take"/> ≤ 0 returns the full catalog.</summary>
        Task<List<RecommendationResponse>> GetRecommendationsAsync(int take = 10);
    }
}
