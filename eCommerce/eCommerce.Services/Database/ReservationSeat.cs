using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerce.Services.Database
{
    /// <summary>
    /// A single seat reserved within a reservation. A unique index on
    /// (ScreeningId, SeatId) guarantees the same physical seat cannot be booked
    /// twice for the same screening (double-booking prevention).
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

        // Screening the seat is reserved for (denormalised for the uniqueness constraint)
        public int ScreeningId { get; set; }

        [ForeignKey("ScreeningId")]
        public Screening Screening { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
    }
}
