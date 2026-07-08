using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eCommerce.Services.Database
{
    public class Hall
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public ScreenType ScreenType { get; set; } = ScreenType.Standard;

        public HallStatus Status { get; set; } = HallStatus.Active;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property for seats in this hall
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();

        // Navigation property for screenings scheduled in this hall
        public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
    }

    public enum ScreenType
    {
        Standard,
        IMAX,

        /// <summary>Displayed as "3D" in the apps.</summary>
        ThreeD
    }

    public enum HallStatus
    {
        Active,
        Maintenance,
        Inactive
    }
}
