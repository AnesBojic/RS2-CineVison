namespace CineVision.Model.Responses
{
    public class AssetResponse
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int MovieId { get; set; }
    }
}
