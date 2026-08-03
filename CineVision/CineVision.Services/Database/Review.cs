using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineVision.Services.Database
{
    /// <summary>
    /// A customer's rating (1-5) and optional comment for a movie. A unique index on
    /// (UserId, MovieId) enforces a single review per user per movie.
    /// </summary>
    public class Review
    {
        [Key]
        public int Id { get; set; }

        // Author of the review
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        // Movie being reviewed
        public int MovieId { get; set; }

        [ForeignKey("MovieId")]
        public Movie Movie { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
