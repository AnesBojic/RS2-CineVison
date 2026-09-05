using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CineVision.Model.Enums;

namespace CineVision.Services.Database
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
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

        public int ProjectionId { get; set; }

        [ForeignKey("ProjectionId")]
        public Projection Projection { get; set; } = null!;

        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [MaxLength(200)]
        public string? CustomerEmail { get; set; }

        /// <summary>Online (Stripe) or Counter; a customer booking is only ever Online.</summary>
        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Online;

        [MaxLength(100)]
        public string? PaymentTransactionId { get; set; }

        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// While set, this row is a seat hold created before the customer is charged: the seats are
        /// blocked for other customers and the hold is released if payment does not arrive in time.
        /// Cleared once the booking is finalised.
        /// </summary>
        public DateTime? HoldExpiresAt { get; set; }

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
}
