using System.Text;
using System.Text.Json;
using eCommerce.Model.Messages;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace eCommerce.WebAPI.Services;

/// <summary>
/// Background worker that consumes queued <see cref="EmailMessage"/> instances from RabbitMQ
/// and delivers them via SMTP (MailKit). Fully resilient: if RabbitMQ or SMTP is unavailable
/// the app still starts and this service keeps retrying without crashing the host.
/// </summary>
public class EmailConsumerBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailConsumerBackgroundService> _logger;

    public EmailConsumerBackgroundService(IConfiguration configuration, ILogger<EmailConsumerBackgroundService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Retry the whole connect/consume setup in a loop so a RabbitMQ outage never crashes the API.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email consumer connection failed; retrying in 10 seconds.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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
            DispatchConsumersAsync = true
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

        _logger.LogInformation("Email consumer connected to RabbitMQ '{Host}:{Port}', listening on queue '{Queue}'.", host, port, queue);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<EmailMessage>(json);

                if (message != null && !string.IsNullOrWhiteSpace(message.To))
                {
                    await SendEmailAsync(message, stoppingToken);
                }
                else
                {
                    _logger.LogWarning("Skipping malformed or empty email message.");
                }

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Don't requeue endlessly on a bad message / SMTP failure — log and drop it.
                _logger.LogError(ex, "Failed to process/send queued email; message dropped.");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);

        // Keep the channel open until the app shuts down or the connection drops.
        while (!stoppingToken.IsCancellationRequested && connection.IsOpen)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task SendEmailAsync(EmailMessage message, CancellationToken stoppingToken)
    {
        var smtp = _configuration.GetSection("Smtp");
        var smtpHost = smtp["Host"];

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogWarning("SMTP is not configured (Smtp:Host missing); email to {To} was consumed but not sent.", message.To);
            return;
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
}
