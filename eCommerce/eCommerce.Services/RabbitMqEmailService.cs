using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using eCommerce.Model.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace eCommerce.Services
{
    /// <summary>
    /// Producer that publishes <see cref="EmailMessage"/> instances as JSON to a durable RabbitMQ queue
    /// using the shared <see cref="IRabbitMqConnection"/>.
    /// </summary>
    public class RabbitMqEmailService : IEmailService
    {
        private readonly IRabbitMqConnection _connection;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqEmailService> _logger;

        public RabbitMqEmailService(
            IRabbitMqConnection connection,
            IConfiguration configuration,
            ILogger<RabbitMqEmailService> logger)
        {
            _connection = connection;
            _configuration = configuration;
            _logger = logger;
        }

        public Task QueueEmailAsync(EmailMessage message)
        {
            var queue = _configuration["RabbitMq:Queue"] ?? "cinevision-emails";

            try
            {
                using var channel = _connection.CreateChannel();

                channel.QueueDeclare(
                    queue: queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";

                channel.BasicPublish(
                    exchange: string.Empty,
                    routingKey: queue,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation(
                    "Queued email to {To} with subject '{Subject}' on queue '{Queue}'.",
                    message.To,
                    message.Subject,
                    queue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue email to {To} on RabbitMQ.", message.To);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
