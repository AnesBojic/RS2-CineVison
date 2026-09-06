using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CineVision.Model;

namespace CineVision.Services.Database
{
    /// <summary>Reference table: named roles with a color and the actions they may perform.</summary>
    public class Role : ILookupEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(7)]
        public string Color { get; set; } = RolePermissionNames.DefaultColor;

        public bool CanAccessDesktop { get; set; }
        public bool CanManageUsers { get; set; }
        public bool CanManageMovies { get; set; }
        public bool CanManageHalls { get; set; }
        public bool CanManageProjections { get; set; }
        public bool CanManageNews { get; set; }
        public bool CanManageReferenceData { get; set; }
        public bool CanManageRoles { get; set; }
        public bool CanViewAnalytics { get; set; }
        public bool CanUseChatBot { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
