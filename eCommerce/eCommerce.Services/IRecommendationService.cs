using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface IRecommendationService
    {
        /// <summary>
        /// Returns personalized scores for active movies. When <paramref name="take"/> is 0 or less,
        /// every active movie is returned (ranked); otherwise returns up to <paramref name="take"/> items.
        /// Movies are ordered by hybrid score but nothing is excluded from the catalog.
        /// </summary>
        Task<List<RecommendationResponse>> GetRecommendationsAsync(int take = 10);
    }
}
