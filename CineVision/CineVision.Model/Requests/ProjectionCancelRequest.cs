namespace CineVision.Model.Requests
{
    public class ProjectionCancelRequest
    {
        /// <summary>Optional reason stored on the projection and on every booking that is refunded.</summary>
        public string? Reason { get; set; }
    }
}
