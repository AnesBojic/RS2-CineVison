namespace CineVision.Model.Requests
{
    public class HallStatusUpdateRequest : LookupRequest
    {
        /// <summary>Halls with this status can host new projections.</summary>
        public bool AllowsScreenings { get; set; }
    }
}
