namespace CineVision.Model.Requests
{
    public class NewsInsertRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
