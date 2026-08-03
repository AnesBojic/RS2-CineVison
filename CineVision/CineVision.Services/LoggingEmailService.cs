using CineVision.Model.Messages;
using Microsoft.Extensions.Logging;

namespace CineVision.Services
{
    /// <summary>
    /// Development fallback when RabbitMQ is disabled â€” logs the email instead of queueing it.
    /// In-app notifications still work; only SMTP delivery is skipped.
    /// </summary>
    public class LoggingEmailService : IEmailService
    {
        private readonly ILogger<LoggingEmailService> _logger;

        public LoggingEmailService(ILogger<LoggingEmailService> logger)
        {
            _logger = logger;
        }

        public Task QueueEmailAsync(EmailMessage message)
        {
            _logger.LogInformation(
                "RabbitMQ disabled â€” email logged only. To: {To}, Subject: {Subject}",
                message.To,
                message.Subject);
            return Task.CompletedTask;
        }
    }
}
