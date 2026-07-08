using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerce.Services.Database
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(30)]
        public string ReservationNumber { get; set; } = string.Empty;

        [Required]
        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;

        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Customer who made the reservation
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        // Screening being reserved
        public int ScreeningId { get; set; }

        [ForeignKey("ScreeningId")]
        public Screening Screening { get; set; } = null!;

        // Guest contact details captured at checkout (shown on the booking confirmation screen).
        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [MaxLength(200)]
        public string? CustomerEmail { get; set; }

        // Payment information
        [MaxLength(100)]
        public string? PaymentTransactionId { get; set; }

        public DateTime? PaymentDate { get; set; }

        // Navigation property for the reserved seats
        public ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();
    }

    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Paid,
        Cancelled
    }
}
