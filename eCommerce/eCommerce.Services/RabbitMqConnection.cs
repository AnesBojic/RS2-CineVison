using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace eCommerce.Services;

/// <summary>
/// Shared RabbitMQ connection for the API process. Registered as a singleton so publish
/// calls do not open a new TCP connection every time.
/// </summary>
public interface IRabbitMqConnection : IDisposable
{
    bool IsConnected { get; }

    IModel CreateChannel();
}

public sealed class RabbitMqConnection : IRabbitMqConnection
{
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly object _sync = new();
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnection(IConfiguration configuration, ILogger<RabbitMqConnection> logger)
    {
        _logger = logger;
        var section = configuration.GetSection("RabbitMq");
        _host = section["Host"] ?? "localhost";
        _port = int.TryParse(section["Port"], out var p) ? p : 5672;
        _username = section["Username"] ?? "guest";
        _password = section["Password"] ?? "guest";
    }

    public bool IsConnected => _connection is { IsOpen: true };

    public IModel CreateChannel()
    {
        EnsureConnected();
        return _connection!.CreateModel();
    }

    private void EnsureConnected()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsConnected)
        {
            return;
        }

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (IsConnected)
            {
                return;
            }

            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = _host,
                Port = _port,
                UserName = _username,
                Password = _password,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _logger.LogInformation("Opened shared RabbitMQ connection to {Host}:{Port}.", _host, _port);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _connection?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error while closing RabbitMQ connection.");
            }

            _connection?.Dispose();
            _connection = null;
        }
    }
}
