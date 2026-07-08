namespace eCommerce.Model.Responses
{
    public class ReservationResponse
    {
        public int Id { get; set; }
        public string ReservationNumber { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int UserId { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }

        public int ScreeningId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public DateTime ScreeningStartTime { get; set; }

        public string? PaymentTransactionId { get; set; }
        public DateTime? PaymentDate { get; set; }

        public List<ReservationSeatResponse> Seats { get; set; } = new();
    }
}
