namespace CineVision.Model.Requests
{
    public class NewsUpdateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
