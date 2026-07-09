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
    /// Producer that publishes <see cref="EmailMessage"/> instances as JSON to a durable RabbitMQ queue.
    /// Connection settings are read from the "RabbitMq" configuration section.
    /// </summary>
    public class RabbitMqEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqEmailService> _logger;

        public RabbitMqEmailService(IConfiguration configuration, ILogger<RabbitMqEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task QueueEmailAsync(EmailMessage message)
        {
            var section = _configuration.GetSection("RabbitMq");
            var host = section["Host"] ?? "localhost";
            var port = int.TryParse(section["Port"], out var p) ? p : 5672;
            var username = section["Username"] ?? "guest";
            var password = section["Password"] ?? "guest";
            var queue = section["Queue"] ?? "cinevision-emails";

            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = username,
                Password = password,
                DispatchConsumersAsync = true
            };

            try
            {
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

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

                _logger.LogInformation("Queued email to {To} with subject '{Subject}' on queue '{Queue}'.", message.To, message.Subject, queue);
            }
            catch (Exception ex)
            {
                // Surface the failure to the caller, but log with context. Callers that must not fail
                // (e.g. reservation confirmation) wrap this in their own try/catch.
                _logger.LogError(ex, "Failed to queue email to {To} on RabbitMQ host '{Host}:{Port}'.", message.To, host, port);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
