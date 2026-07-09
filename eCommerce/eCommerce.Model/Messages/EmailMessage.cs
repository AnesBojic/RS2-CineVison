namespace eCommerce.Model.Messages
{
    /// <summary>
    /// Contract for an email queued to RabbitMQ and consumed by the email background service.
    /// </summary>
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
    }
}
