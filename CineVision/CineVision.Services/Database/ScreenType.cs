using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CineVision.Services.Database
{
    /// <summary>Reference table: screen technology a hall provides (Standard, IMAX, 3Dâ€¦).</summary>
    public class ScreenType : ILookupEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Hall> Halls { get; set; } = new List<Hall>();
    }
}
