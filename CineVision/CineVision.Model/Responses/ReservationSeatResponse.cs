namespace CineVision.Model.Responses
{
    public class ReservationSeatResponse
    {
        public int Id { get; set; }
        public int SeatId { get; set; }
        public string RowLabel { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public int SeatType { get; set; }
        public decimal Price { get; set; }
    }
}
