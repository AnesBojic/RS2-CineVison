namespace CineVision.Model.SearchObjects
{
    public class AssetSearch : BaseSearchObject
    {
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public int? MovieId { get; set; }
    }
}
