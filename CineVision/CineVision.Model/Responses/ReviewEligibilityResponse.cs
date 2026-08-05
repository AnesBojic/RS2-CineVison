namespace CineVision.Model.Responses
{
    public class ReviewEligibilityResponse
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;

        /// <summary>User attended a past paid/confirmed projection and has not reviewed yet.</summary>
        public bool CanReview { get; set; }

        public bool HasReview { get; set; }
        public int? ExistingReviewId { get; set; }
    }
}
