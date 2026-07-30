using System;
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

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int ScreeningId { get; set; }

        [ForeignKey("ScreeningId")]
        public Screening Screening { get; set; } = null!;

        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [MaxLength(200)]
        public string? CustomerEmail { get; set; }

        [MaxLength(100)]
        public string? PaymentTransactionId { get; set; }

        public DateTime? PaymentDate { get; set; }

        /// <summary>User who cancelled (customer or admin).</summary>
        public int? CancelledByUserId { get; set; }

        [ForeignKey(nameof(CancelledByUserId))]
        public User? CancelledByUser { get; set; }

        public DateTime? CancelledAt { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public DateTime? CompletedAt { get; set; }

        public ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();
    }

    public enum ReservationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Paid = 2,
        Cancelled = 3,
        Completed = 4
    }
}
