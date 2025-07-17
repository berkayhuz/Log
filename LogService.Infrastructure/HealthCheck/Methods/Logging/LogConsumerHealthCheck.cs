namespace LogService.Infrastructure.HealthCheck.Methods.Logging;
using System;
using System.Threading.Tasks;

using LogService.Application.Options;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

[Name("rabbitmq_consumer_check")]
[HealthTags("rabbitmq", "consumer", "queue", "connection", "ready")]
public class LogConsumerHealthCheck(IOptions<RabbitMqSettings> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(null, cancellationToken);

            // Queue declare — yoksa oluşturur, varsa dokunmaz (passive kontrol gibi)
            await channel.QueueDeclareAsync(
                queue: settings.LogQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken
            );

            return HealthCheckResult.Healthy("RabbitMQ consumer connection and queue are healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ consumer failed to connect or declare queue.", ex);
        }
    }
}
