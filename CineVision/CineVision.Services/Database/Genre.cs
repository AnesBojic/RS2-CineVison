using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CineVision.Services.Database
{
    public class Genre
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property for movies in this genre
        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
