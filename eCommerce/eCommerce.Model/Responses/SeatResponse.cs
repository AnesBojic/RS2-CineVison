namespace eCommerce.Model.Responses
{
    public class SeatResponse
    {
        public int Id { get; set; }
        public int HallId { get; set; }
        public string RowLabel { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        /// <summary>0 = Regular, 2 = Couple.</summary>
        public int SeatType { get; set; }

        public int? PartnerSeatId { get; set; }

        /// <summary>Seats occupied when booked (2 for couple loveseats).</summary>
        public int SpotsOccupied { get; set; } = 1;

        public bool IsActive { get; set; }
    }
}
