namespace CineVision.Model.Responses
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

        public int ProjectionId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public DateTime ProjectionStartTime { get; set; }
        public DateTime ProjectionEndTime { get; set; }

        public string? PaymentTransactionId { get; set; }
        public DateTime? PaymentDate { get; set; }

        public int? CancelledByUserId { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CompletedAt { get; set; }

        public List<ReservationSeatResponse> Seats { get; set; } = new();
    }
}
