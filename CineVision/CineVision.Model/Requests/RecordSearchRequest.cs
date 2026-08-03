using System.ComponentModel.DataAnnotations;

namespace CineVision.Model.Requests
{
    /// <summary>
    /// Records a customer search for the recommendation engine (SearchHistories).
    /// Used when the UI filters client-side and does not hit GET /Movies.
    /// </summary>
    public class RecordSearchRequest
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        public int? GenreId { get; set; }
    }
}
