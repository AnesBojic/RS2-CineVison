using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerce.Services.Database
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        [MaxLength(100)]
        public string? Director { get; set; }

        public DateTime? ReleaseDate { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        [MaxLength(20)]
        public string? AgeRating { get; set; }

        [MaxLength(500)]
        public string? TrailerUrl { get; set; }

        /// <summary>Movie poster image stored as a base64 string (same pattern as user profile images).</summary>
        public string? PosterImageBase64 { get; set; }

        public bool IsActive { get; set; } = true;

        // Number of times the movie's details have been opened (popularity signal for recommendations).
        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(1000)]
        public string MovieState { get; set; } = string.Empty;

        // Genre relationship
        public int? GenreId { get; set; }

        [ForeignKey("GenreId")]
        public Genre? Genre { get; set; }

        // Navigation property for poster/promo images
        public ICollection<Asset> Assets { get; set; } = new List<Asset>();

        // Navigation property for screenings of this movie
        public ICollection<Screening> Screenings { get; set; } = new List<Screening>();

        // Navigation property for customer reviews of this movie
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
