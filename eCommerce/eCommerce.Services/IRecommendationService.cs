using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface IRecommendationService
    {
        /// <summary>
        /// Returns up to <paramref name="take"/> personalized movie recommendations for the current
        /// user, combining popularity-based and content-based scores.
        /// </summary>
        Task<List<RecommendationResponse>> GetRecommendationsAsync(int take = 10);
    }
}
