using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CineVision.Services.Database
{
    /// <summary>Reference table: application roles assigned to users (Admin, Staff, Customer…).</summary>
    public class Role : ILookupEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
