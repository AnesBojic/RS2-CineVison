using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineVision.Services.Database
{
    /// <summary>
    /// A single seat reserved within a reservation. A filtered unique index on
    /// (ProjectionId, SeatId) WHERE ReleasedAt IS NULL guarantees the same physical seat cannot be
    /// booked twice for the same projection, while released rows stay as booking history.
    /// </summary>
    public class ReservationSeat
    {
        [Key]
        public int Id { get; set; }

        // Reservation that this seat belongs to
        public int ReservationId { get; set; }

        [ForeignKey("ReservationId")]
        public Reservation Reservation { get; set; } = null!;

        // Reserved seat
        public int SeatId { get; set; }

        [ForeignKey("SeatId")]
        public Seat Seat { get; set; } = null!;

        // Projection the seat is reserved for (denormalised for the uniqueness constraint)
        public int ProjectionId { get; set; }

        [ForeignKey("ProjectionId")]
        public Projection Projection { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// When the seat stopped being occupied (cancellation or expired hold). The row is kept so
        /// the exact seats and prices of a booking remain on record; availability ignores it.
        /// </summary>
        public DateTime? ReleasedAt { get; set; }
    }
}
