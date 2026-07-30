using System.Text;
using System.Text.Json;
using eCommerce.Model.Messages;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace eCommerce.Worker.Services;

/// <summary>
/// Separate-process consumer: pulls <see cref="EmailMessage"/> from RabbitMQ and sends via SMTP.
/// Connection failures use exponential backoff. Per-message failures retry with backoff up to a limit.
/// </summary>
public sealed class EmailConsumerWorker : BackgroundService
{
    private const int MaxMessageAttempts = 4;
    private const string RetryHeader = "x-retry-count";

    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailConsumerWorker> _logger;

    public EmailConsumerWorker(IConfiguration configuration, ILogger<EmailConsumerWorker> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                attempt = 0;
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                attempt++;
                var delay = GetBackoff(attempt);
                _logger.LogError(
                    ex,
                    "Email worker connection failed (attempt {Attempt}); retrying in {DelaySeconds}s.",
                    attempt,
                    delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
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
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation(
            "Email worker connected to RabbitMQ '{Host}:{Port}', listening on queue '{Queue}'.",
            host,
            port,
            queue);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            var retryCount = ReadRetryCount(ea.BasicProperties);

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<EmailMessage>(json);

                if (message == null || string.IsNullOrWhiteSpace(message.To))
                {
                    _logger.LogWarning("Skipping malformed or empty email message.");
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                await SendEmailAsync(message, stoppingToken);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                var nextAttempt = retryCount + 1;

                if (nextAttempt >= MaxMessageAttempts)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process/send queued email after {Attempts} attempts; message dropped.",
                        nextAttempt);
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                var delay = GetBackoff(nextAttempt);
                _logger.LogWarning(
                    ex,
                    "Email send failed (attempt {Attempt}/{Max}); requeue after {DelaySeconds}s.",
                    nextAttempt,
                    MaxMessageAttempts,
                    delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    return;
                }

                // Republish with incremented retry header, then ack the original delivery.
                var props = channel.CreateBasicProperties();
                props.Persistent = true;
                props.ContentType = "application/json";
                props.Headers = ea.BasicProperties.Headers != null
                    ? new Dictionary<string, object>(ea.BasicProperties.Headers)
                    : new Dictionary<string, object>();
                props.Headers[RetryHeader] = nextAttempt;

                channel.BasicPublish(
                    exchange: string.Empty,
                    routingKey: queue,
                    basicProperties: props,
                    body: ea.Body);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
        };

        channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);

        while (!stoppingToken.IsCancellationRequested && connection.IsOpen)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }

        if (!connection.IsOpen && !stoppingToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("RabbitMQ connection closed unexpectedly.");
        }
    }

    private async Task SendEmailAsync(EmailMessage message, CancellationToken stoppingToken)
    {
        var smtp = _configuration.GetSection("Smtp");
        var smtpHost = smtp["Host"];

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            throw new InvalidOperationException("SMTP is not configured (Smtp:Host missing).");
        }

        var smtpPort = int.TryParse(smtp["Port"], out var sp) ? sp : 587;
        var smtpUser = smtp["User"];
        var smtpPassword = smtp["Password"];
        var fromEmail = smtp["FromEmail"] ?? "no-reply@cinevision.local";
        var fromName = smtp["FromName"] ?? "CineVision";
        var useSsl = bool.TryParse(smtp["UseSsl"], out var ssl) && ssl;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(fromName, fromEmail));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder();
        if (message.IsHtml)
        {
            bodyBuilder.HtmlBody = message.Body;
        }
        else
        {
            bodyBuilder.TextBody = message.Body;
        }

        mime.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(smtpHost, smtpPort, socketOptions, stoppingToken);

        if (!string.IsNullOrWhiteSpace(smtpUser))
        {
            await client.AuthenticateAsync(smtpUser, smtpPassword, stoppingToken);
        }

        await client.SendAsync(mime, stoppingToken);
        await client.DisconnectAsync(true, stoppingToken);

        _logger.LogInformation("Sent email to {To} with subject '{Subject}'.", message.To, message.Subject);
    }

    private static int ReadRetryCount(IBasicProperties properties)
    {
        if (properties.Headers == null || !properties.Headers.TryGetValue(RetryHeader, out var raw) || raw == null)
        {
            return 0;
        }

        return raw switch
        {
            int i => i,
            long l => (int)l,
            byte b => b,
            byte[] bytes when bytes.Length > 0 => bytes[0],
            _ => int.TryParse(raw.ToString(), out var parsed) ? parsed : 0
        };
    }

    /// <summary>1s → 2s → 4s → 8s … capped at 30s.</summary>
    private static TimeSpan GetBackoff(int attempt)
    {
        var seconds = Math.Min(30, Math.Pow(2, Math.Max(0, attempt - 1)));
        return TimeSpan.FromSeconds(seconds);
    }
}
