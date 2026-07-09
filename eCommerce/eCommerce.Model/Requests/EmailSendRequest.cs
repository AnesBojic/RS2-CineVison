namespace eCommerce.Model.Requests
{
    /// <summary>
    /// Payload for POST /Users/{id}/SendEmail — an admin composing an email to a user.
    /// </summary>
    public class EmailSendRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
    }
}
