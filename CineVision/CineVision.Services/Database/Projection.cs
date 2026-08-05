using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineVision.Services.Database
{
    public class Projection
    {
        [Key]
        public int Id { get; set; }

        // Movie relationship
        public int MovieId { get; set; }

        [ForeignKey("MovieId")]
        public Movie Movie { get; set; } = null!;

        // Hall relationship
        public int HallId { get; set; }

        [ForeignKey("HallId")]
        public Hall Hall { get; set; } = null!;

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        // Spoken language of this projection (reference table)
        public int? LanguageId { get; set; }

        [ForeignKey("LanguageId")]
        public Language? Language { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property for reservations of this projection
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        // Navigation property for individual reserved seats of this projection
        public ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();
    }
}
