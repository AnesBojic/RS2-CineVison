using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CineVision.Model.Enums;

namespace CineVision.Services.Database
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

        public DateTime? ReleaseDate { get; set; }

        // Spoken language (reference table)
        public int? LanguageId { get; set; }

        [ForeignKey("LanguageId")]
        public Language? Language { get; set; }

        // Content/age rating (reference table)
        public int? AgeRatingId { get; set; }

        [ForeignKey("AgeRatingId")]
        public AgeRating? AgeRating { get; set; }

        /// <summary>Movie poster image stored as a base64 string (same pattern as user profile images).</summary>
        public string? PosterImageBase64 { get; set; }

        public bool IsActive { get; set; } = true;

        // Number of times the movie's details have been opened (popularity signal for recommendations).
        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public MovieLifecycleState MovieState { get; set; } = MovieLifecycleState.Draft;

        // Genre relationship
        public int? GenreId { get; set; }

        [ForeignKey("GenreId")]
        public Genre? Genre { get; set; }

        // Navigation property for projections of this movie
        public ICollection<Projection> Projections { get; set; } = new List<Projection>();

        // Navigation property for customer reviews of this movie
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
