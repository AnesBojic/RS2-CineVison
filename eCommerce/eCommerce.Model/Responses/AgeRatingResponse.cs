namespace eCommerce.Model.Responses
{
    public class AgeRatingResponse : LookupResponse
    {
        /// <summary>Minimum viewer age, when the rating defines one.</summary>
        public int? MinimumAge { get; set; }
    }
}
