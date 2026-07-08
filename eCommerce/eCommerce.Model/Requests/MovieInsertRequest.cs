using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class MovieInsertRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Movie runtime in minutes.</summary>
        public int DurationMinutes { get; set; }

        public int? GenreId { get; set; }

        [MaxLength(100)]
        public string? Director { get; set; }

        public DateTime? ReleaseDate { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        /// <summary>Content/age rating, e.g. "PG-13".</summary>
        [MaxLength(20)]
        public string? AgeRating { get; set; }

        [MaxLength(500)]
        public string? TrailerUrl { get; set; }

        /// <summary>Movie poster as a base64-encoded image (e.g. from the "upload poster" field).</summary>
        public string? PosterImageBase64 { get; set; }

        public bool IsActive { get; set; } = true;

        public List<AssetInsertRequest>? Assets { get; set; }
    }
}
