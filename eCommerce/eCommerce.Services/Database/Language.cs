using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eCommerce.Services.Database
{
    /// <summary>Reference table: spoken language of a movie or a single projection.</summary>
    public class Language : ILookupEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(80)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Short ISO-style code, e.g. "en".</summary>
        [MaxLength(10)]
        public string? Code { get; set; }

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Movie> Movies { get; set; } = new List<Movie>();

        public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
    }
}
