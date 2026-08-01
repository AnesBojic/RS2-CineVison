namespace eCommerce.Model.Requests
{
    public class AgeRatingUpdateRequest : LookupRequest
    {
        /// <summary>Minimum viewer age, when the rating defines one.</summary>
        public int? MinimumAge { get; set; }
    }
}
