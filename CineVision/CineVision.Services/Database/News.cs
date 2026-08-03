using System;
using System.ComponentModel.DataAnnotations;

namespace CineVision.Services.Database
{
    /// <summary>
    /// Announcements / news shown to customers (title, body, optional image, published time).
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

        /// <summary>Optional base64 image for the article.</summary>
        public string? ImageBase64 { get; set; }

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
