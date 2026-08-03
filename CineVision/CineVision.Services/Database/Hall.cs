using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineVision.Services.Database
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

        // Screen technology (reference table)
        public int ScreenTypeId { get; set; }

        [ForeignKey("ScreenTypeId")]
        public ScreenType? ScreenType { get; set; }

        // Operational status (reference table)
        public int StatusId { get; set; }

        [ForeignKey("StatusId")]
        public HallStatus? Status { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property for seats in this hall
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();

        // Navigation property for screenings scheduled in this hall
        public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
    }
}
