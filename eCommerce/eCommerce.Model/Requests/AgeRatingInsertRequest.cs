namespace eCommerce.Model.Requests
{
    public class AgeRatingInsertRequest : LookupRequest
    {
        /// <summary>Minimum viewer age, when the rating defines one.</summary>
        public int? MinimumAge { get; set; }
    }
}
