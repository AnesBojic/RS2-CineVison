namespace eCommerce.Model.Responses
{
    public class MovieResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int? GenreId { get; set; }
        public string? Director { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? Language { get; set; }
        public string? AgeRating { get; set; }
        public string? TrailerUrl { get; set; }
        public string? PosterImageBase64 { get; set; }
        public bool IsActive { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string MovieState { get; set; } = string.Empty;
        public GenreResponse? Genre { get; set; }
        public List<string> AllowedActions { get; set; } = new List<string>();
        public List<AssetResponse> Assets { get; set; } = new List<AssetResponse>();
    }
}
