using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IReviewService : IBaseReadService<ReviewResponse, ReviewSearchObject>
    {
        Task<ReviewResponse> InsertAsync(ReviewInsertRequest request);

        Task<ReviewResponse> UpdateAsync(int id, ReviewUpdateRequest request);

        Task DeleteAsync(int id);

        Task<List<ReviewEligibilityResponse>> GetMyEligibilityAsync();
    }
}
