using System.ComponentModel.DataAnnotations;

namespace CineVision.Model.Requests
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

        public DateTime? ReleaseDate { get; set; }

        /// <summary>Id of a row in the Languages reference table.</summary>
        public int? LanguageId { get; set; }

        /// <summary>Id of a row in the AgeRatings reference table.</summary>
        public int? AgeRatingId { get; set; }

        /// <summary>Movie poster as a base64-encoded image (e.g. from the "upload poster" field).</summary>
        public string? PosterImageBase64 { get; set; }
    }
}
