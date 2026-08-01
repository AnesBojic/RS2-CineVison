using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerce.Services.Enums;

namespace eCommerce.Services.Database
{
    public class Seat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(5)]
        public string RowLabel { get; set; } = string.Empty;

        public int SeatNumber { get; set; }

        public SeatType SeatType { get; set; } = SeatType.Regular;

        /// <summary>
        /// When <see cref="SeatType"/> is Couple, the adjacent seat blocked by this loveseat.
        /// </summary>
        public int? PartnerSeatId { get; set; }

        [ForeignKey("PartnerSeatId")]
        public Seat? PartnerSeat { get; set; }

        public bool IsActive { get; set; } = true;

        // Hall relationship
        public int HallId { get; set; }

        [ForeignKey("HallId")]
        public Hall Hall { get; set; } = null!;

        // Navigation property for reservation seats referencing this seat
        public ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();
    }
}
