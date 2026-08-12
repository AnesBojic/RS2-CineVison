namespace CineVision.Model.SearchObjects
{
    /// <summary>
    /// Filter used by every reference-data endpoint (screen types, hall statuses,
    /// age ratings, languages).
    /// </summary>
    public class LookupSearchObject : BaseSearchObject
    {
        /// <summary>Substring to match against the name (case-insensitive).</summary>
        public string? Name { get; set; }
    }
}
