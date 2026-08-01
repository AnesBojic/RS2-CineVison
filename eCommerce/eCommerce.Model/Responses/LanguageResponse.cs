namespace eCommerce.Model.Responses
{
    public class LanguageResponse : LookupResponse
    {
        /// <summary>Short ISO-style code, e.g. "en".</summary>
        public string? Code { get; set; }
    }
}
