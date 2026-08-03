namespace CineVision.Model.Requests
{
    public class ReservationCancelRequest
    {
        /// <summary>Optional reason recorded in the audit trail and shown to admins.</summary>
        public string? Reason { get; set; }
    }
}
