using CineVision.Model.Responses;

namespace CineVision.Services
{
    public interface IRecommendationService
    {
        /// <summary>Ranked active movies; <paramref name="take"/> â‰¤ 0 returns the full catalog.</summary>
        Task<List<RecommendationResponse>> GetRecommendationsAsync(int take = 10);
    }
}
