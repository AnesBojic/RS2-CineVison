namespace CineVision.Model.Responses
{
    public class HallStatusResponse : LookupResponse
    {
        /// <summary>Halls with this status can host new projections.</summary>
        public bool AllowsScreenings { get; set; }
    }
}
