using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eCommerce.Services.Database
{
    /// <summary>Reference table: operational status of a hall (Active, Maintenance, Inactive…).</summary>
    public class HallStatus : ILookupEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Drives scheduling: only halls whose status allows screenings can host new projections.
        /// Keeping the rule on the row means staff can add or rename statuses without code changes.
        /// </summary>
        public bool AllowsScreenings { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Hall> Halls { get; set; } = new List<Hall>();
    }
}
