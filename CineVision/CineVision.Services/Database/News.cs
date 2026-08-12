using System;
using System.ComponentModel.DataAnnotations;

namespace CineVision.Services.Database
{
    /// <summary>
    /// Announcements / news shown to customers (title, body, image, published time).
    /// </summary>
    public class News
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>News image as base64 (same pattern as movie posters).</summary>
        public string? ImageBase64 { get; set; }

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
