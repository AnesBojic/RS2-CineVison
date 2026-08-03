using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;

namespace CineVision.Services
{
    public interface IReviewService : IBaseReadService<ReviewResponse, ReviewSearchObject>
    {
        Task<ReviewResponse> InsertAsync(ReviewInsertRequest request);

        Task<ReviewResponse> UpdateAsync(int id, ReviewUpdateRequest request);

        Task DeleteAsync(int id);

        Task<List<ReviewEligibilityResponse>> GetMyEligibilityAsync();
    }
}
