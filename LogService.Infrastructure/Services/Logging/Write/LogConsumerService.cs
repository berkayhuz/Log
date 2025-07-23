namespace LogService.Infrastructure.Services.Logging.Write;

using System.Text;
using System.Text.Json;

using LogService.Application.Options;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Fallback.Abstractions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;

public class LogConsumerService : BackgroundService
{
    private readonly IResilientLogWriter _resilientLogWriter;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<LogConsumerService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public LogConsumerService(
        IResilientLogWriter resilientLogWriter,
        IOptions<RabbitMqSettings> rabbitOptions,
        ILogger<LogConsumerService> logger)
    {
        _resilientLogWriter = resilientLogWriter;
        _settings = rabbitOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
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
            var result = await ProcessMessageAsync(ea);
            if (result.IsSuccess)
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            else
            {
                _logger.LogWarning("⛔ Log işlenemedi, requeue={Requeue}. Hatalar: {Errors}", result.Metadata?["Requeue"], string.Join(" | ", result.Errors));
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: (bool)(result.Metadata?["Requeue"] ?? true));
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _settings.LogQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );
    }

    private async Task<Result> ProcessMessageAsync(BasicDeliverEventArgs ea)
    {
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            var dto = JsonSerializer.Deserialize<LogEntryDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dto is null)
            {
                return Result.Failure("Deserialization sonucu null.")
                    .WithErrorCode(ErrorCode.SerializationFailure)
                    .WithErrorType(ErrorType.Serialization)
                    .WithMetadata("Raw", json)
                    .WithMetadata("Requeue", false);
            }

            var result = await _resilientLogWriter.WriteWithRetryAsync(dto);
            return result.IsFailure
                ? result.WithMetadata("Requeue", true)
                : result;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "🚫 Geçersiz JSON. Discarding. Raw: {Raw}", json);
            return Result.Failure("JSON parse hatası: " + jsonEx.Message)
                .WithException(jsonEx)
                .WithErrorCode(ErrorCode.SerializationFailure)
                .WithErrorType(ErrorType.Serialization)
                .WithMetadata("Raw", json)
                .WithMetadata("Requeue", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 Log mesajı işlenirken beklenmeyen hata");
            return Result.Failure("Log mesajı işlenirken hata: " + ex.Message)
                .WithException(ex)
                .WithErrorType(ErrorType.Unexpected)
                .WithMetadata("Requeue", true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync(cancellationToken);
                await _channel.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ RabbitMQ channel kapanırken hata.");
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
