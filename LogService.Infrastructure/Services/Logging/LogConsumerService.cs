namespace LogService.Infrastructure.Services.Logging;
using System.Text;
using System.Text.Json;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Options;
using LogService.Application.Resilience;
using LogService.SharedKernel.DTOs;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Keys;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class LogConsumerService(
    IResilientLogWriter resilientLogWriter,
    IOptions<RabbitMqSettings> rabbitOptions,
    ILogServiceLogger logLogger) : BackgroundService
{
    private readonly RabbitMqSettings _settings = rabbitOptions.Value;
    private readonly ILogServiceLogger _logLogger = logLogger;

    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string className = nameof(LogConsumerService);

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

            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            LogEntryDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<LogEntryDto>(json);
            }
            catch (Exception ex)
            {
                await _logLogger.LogAsync(LogStage.Warning, "RabbitMQ'dan gelen mesaj deserialize edilemedi.");
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            if (dto is null)
            {
                await _logLogger.LogAsync(LogStage.Warning, LogMessageDefaults.Messages[LogMessageKeys.Log_InvalidMessage]);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            var result = await resilientLogWriter.WriteWithRetryAsync(dto);

            if (result.IsFailure)
            {
                var errorMessage = LogMessageDefaults.Messages[LogMessageKeys.Elastic_WriteErrorList]
                    .Replace("{Errors}", string.Join(", ", result.Errors));

                await _logLogger.LogAsync(LogStage.Error, errorMessage);
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                return;
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
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
        const string className = nameof(LogConsumerService);

        if (_channel != null)
        {
            await _channel.CloseAsync();
        }

        _connection?.Dispose();
    }
}
