using System.ComponentModel.DataAnnotations;

namespace CineVision.Model.Requests
{
    public class LanguageInsertRequest : LookupRequest
    {
        /// <summary>Short ISO-style code, e.g. "en".</summary>
        [MaxLength(10)]
        public string? Code { get; set; }
    }
}
