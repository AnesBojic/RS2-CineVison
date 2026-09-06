namespace CineVision.Model.Requests
{
    public class ReservationCancelRequest
    {
        /// <summary>Required reason stored in the audit trail (CancellationReason).</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
