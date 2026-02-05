using System.Text;
using System.Text.Json;
using IoTSensorMonitoring.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace IoTSensorMonitoring.Infrastructure.Messaging;

public class PublicadorRabbitMq : IPublicadorMensagens, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchange;
    private readonly ILogger<PublicadorRabbitMq> _logger;
    private bool _disposed;

    public PublicadorRabbitMq(IConfiguration config, ILogger<PublicadorRabbitMq> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest",
            VirtualHost = config["RabbitMQ:VirtualHost"] ?? "/"
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _exchange = config["RabbitMQ:Exchange"] ?? "iot.medicoes";

        _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true, autoDelete: false)
            .GetAwaiter().GetResult();

        _logger.LogInformation("Publicador RabbitMQ inicializado. Exchange: {Exchange}", _exchange);
    }

    public void Publicar<T>(string routingKey, T mensagem) where T : class
    {
        PublicarAsync(routingKey, mensagem).GetAwaiter().GetResult();
    }

    public async Task PublicarAsync<T>(string routingKey, T mensagem, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(mensagem);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: _exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken
            );

            _logger.LogDebug("Mensagem publicada. RoutingKey: {RoutingKey}", routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem. RoutingKey: {RoutingKey}", routingKey);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _channel.CloseAsync();
        await _channel.DisposeAsync();
        await _connection.CloseAsync();
        await _connection.DisposeAsync();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
