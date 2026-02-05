using System.Text;
using System.Text.Json;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IoTSensorMonitoring.BackgroundWorkers.Workers;

public class ConsumidorMedicoes : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumidorMedicoes> _logger;

    public ConsumidorMedicoes(
        IConfiguration config,
        IServiceProvider serviceProvider,
        ILogger<ConsumidorMedicoes> logger)
    {
        _serviceProvider = serviceProvider;
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

        var exchange = config["RabbitMQ:Exchange"] ?? "iot.medicoes";
        _queue = config["RabbitMQ:Queue"] ?? "medicoes.processamento";
        var routingKey = config["RabbitMQ:RoutingKey"] ?? "medicao.#";

        _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false)
            .GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(_queue, durable: true, exclusive: false, autoDelete: false, arguments: null)
            .GetAwaiter().GetResult();
        _channel.QueueBindAsync(_queue, exchange, routingKey, arguments: null)
            .GetAwaiter().GetResult();
        _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false)
            .GetAwaiter().GetResult();

        _logger.LogInformation("Consumidor RabbitMQ inicializado. Queue: {Queue}", _queue);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var mensagem = JsonSerializer.Deserialize<MensagemMedicao>(json);

                if (mensagem != null)
                {
                    await ProcessarMedicaoAsync(mensagem, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                }
                else
                {
                    _logger.LogWarning("Mensagem inválida recebida");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(_queue, autoAck: false, consumer: consumer, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessarMedicaoAsync(MensagemMedicao mensagem, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var servicoAlerta = scope.ServiceProvider.GetRequiredService<ServicoAvaliacaoAlerta>();

        _logger.LogDebug("Processando medição {IdMedicao} do sensor {CodigoSensor}",
            mensagem.IdMedicao, mensagem.CodigoSensor);

        var alertas = await servicoAlerta.AvaliarAlertasAsync(
            mensagem.IdSensor,
            mensagem.ValorMedicao,
            mensagem.DataHoraMedicao,
            cancellationToken);

        if (alertas.Any())
        {
            _logger.LogWarning("Sensor {CodigoSensor} disparou {Count} alerta(s)",
                mensagem.CodigoSensor, alertas.Count);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _channel.CloseAsync(cancellationToken);
        await _channel.DisposeAsync();
        await _connection.CloseAsync(cancellationToken);
        await _connection.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
