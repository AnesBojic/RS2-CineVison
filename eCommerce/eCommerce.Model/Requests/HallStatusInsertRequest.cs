namespace eCommerce.Model.Requests
{
    public class HallStatusInsertRequest : LookupRequest
    {
        /// <summary>Halls with this status can host new projections.</summary>
        public bool AllowsScreenings { get; set; }
    }
}
