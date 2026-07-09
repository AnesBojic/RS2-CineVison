namespace eCommerce.Model.Requests
{
    public class ReviewInsertRequest
    {
        public int MovieId { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
}
