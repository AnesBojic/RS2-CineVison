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
        /// <summary>Computed as start + movie duration (not stored on Projection).</summary>
        public DateTime ProjectionEndTime { get; set; }

        public int PaymentMethod { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;

        /// <summary>Whether money was collected; stays true after cancel or complete.</summary>
        public int PaymentStatus { get; set; }
        public string PaymentStatusName { get; set; } = string.Empty;

        public string? PaymentTransactionId { get; set; }
        public DateTime? PaymentDate { get; set; }

        public int RefundStatus { get; set; }
        public string RefundStatusName { get; set; } = string.Empty;
        public string? RefundId { get; set; }
        public DateTime? RefundedAt { get; set; }

        /// <summary>Error from the last failed refund attempt, if any.</summary>
        public string? RefundError { get; set; }

        /// <summary>Set only while the booking is an unpaid seat hold.</summary>
        public DateTime? HoldExpiresAt { get; set; }

        public int? CancelledByUserId { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CompletedAt { get; set; }

        public List<ReservationSeatResponse> Seats { get; set; } = new();
    }
}
