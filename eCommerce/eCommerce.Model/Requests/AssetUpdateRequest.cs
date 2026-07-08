namespace eCommerce.Model.Requests
{
    public class AssetUpdateRequest
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
        public int MovieId { get; set; }
    }
}
