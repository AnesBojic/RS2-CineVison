namespace CineVision.Model.Responses
{
    /// <summary>
    /// Represents a seat in the context of a specific screening, including whether it has
    /// already been reserved. Used to render the seat-selection map.
    /// </summary>
    public class ScreeningSeatResponse
    {
        public int SeatId { get; set; }
        public int HallId { get; set; }
        public string RowLabel { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        /// <summary>0 = Regular, 2 = Couple.</summary>
        public int SeatType { get; set; }

        public int? PartnerSeatId { get; set; }

        /// <summary>How many grid spots this seat uses (2 for couple).</summary>
        public int SpotsOccupied { get; set; } = 1;

        public bool IsTaken { get; set; }

        /// <summary>Total price for this selection (2Ã— base price for couple seats).</summary>
        public decimal Price { get; set; }
    }
}
