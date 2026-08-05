using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CineVision.Services.Database
{
    /// <summary>Reference table: content/age rating of a movie (G, PG, PG-13, R…).</summary>
    public class AgeRating : ILookupEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Minimum viewer age, when the rating defines one.</summary>
        public int? MinimumAge { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
