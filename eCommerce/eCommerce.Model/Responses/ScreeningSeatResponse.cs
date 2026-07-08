namespace eCommerce.Model.Responses
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
        /// <summary>0 = Regular, 1 = VIP.</summary>
        public int SeatType { get; set; }
        public bool IsTaken { get; set; }
        public decimal Price { get; set; }
    }
}
