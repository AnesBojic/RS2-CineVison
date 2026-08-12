namespace CineVision.Model.Responses
{
    public class ProjectionResponse
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? MoviePosterBase64 { get; set; }
        public int HallId { get; set; }
        public string HallName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        /// <summary>Computed as StartTime + movie duration (not stored).</summary>
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }

        public int? LanguageId { get; set; }

        /// <summary>Name of the referenced language, flattened for display.</summary>
        public string? Language { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }

        public MovieResponse? Movie { get; set; }
        public HallResponse? Hall { get; set; }
    }
}
