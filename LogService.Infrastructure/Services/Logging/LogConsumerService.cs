namespace LogService.Infrastructure.Services.Logging;
using System.Text;
using System.Text.Json;

using LogService.Application.Options;
using LogService.Application.Resilience;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class LogConsumerService(
    IResilientLogWriter resilientLogWriter,
    IOptions<RabbitMqSettings> rabbitOptions,
    ILogger<LogConsumerService> logger) : BackgroundService
{
    private readonly RabbitMqSettings _settings = rabbitOptions.Value;

    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(null, stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _settings.LogQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            await TryCatch.ExecuteAsync(
                tryFunc: async () =>
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var dto = JsonSerializer.Deserialize<LogEntryDto>(json);

                    if (dto is null)
                    {
                        logger.LogWarning("Deserialized log entry is null. Raw: {Json}", json);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    var result = await resilientLogWriter.WriteWithRetryAsync(dto);

                    if (result.IsFailure)
                    {
                        logger.LogWarning("Failed to write log entry. Requeueing. Reason: {Reason}", result.IsFailure);
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    }
                    else
                    {
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                },
                logger: logger,
                context: "LogConsumerService.ReceivedAsync"
            );
        };

        await _channel.BasicConsumeAsync(
            queue: _settings.LogQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.CloseAsync();

        _connection?.Dispose();
    }
}
