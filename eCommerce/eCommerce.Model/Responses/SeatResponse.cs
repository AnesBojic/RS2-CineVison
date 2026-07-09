namespace eCommerce.Model.Responses
{
    public class SeatResponse
    {
        public int Id { get; set; }
        public int HallId { get; set; }
        public string RowLabel { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        /// <summary>0 = Regular, 1 = VIP.</summary>
        public int SeatType { get; set; }
        public bool IsActive { get; set; }
    }
}
