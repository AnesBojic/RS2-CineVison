namespace CineVision.Model.Requests
{
    public class HallStatusInsertRequest : LookupRequest
    {
        /// <summary>Halls with this status can host new projections.</summary>
        public bool AllowsProjections { get; set; }
    }
}
