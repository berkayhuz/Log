namespace LogService.Infrastructure.Services.Logging;

using LogService.Application.Abstractions.Logging;
using LogService.Infrastructure.Services.Logging.Abstractions;
using LogService.Infrastructure.Services.Logging.Read;
using LogService.Infrastructure.Services.Logging.Write;

using Microsoft.Extensions.DependencyInjection;

public static class LoggingModule
{
    public static IServiceCollection AddLoggingServices(this IServiceCollection services)
    {
        return services
            .AddScoped<ILogEntryWriteService, LogEntryWriteService>()
            .AddScoped<IBulkLogEntryWriteService, BulkLogEntryWriteService>()
            .AddScoped<ILogQueryService, LogQueryService>()
            .AddScoped<IElasticClientAdapter, ElasticClientAdapter>()
            .AddHostedService<LogConsumerService>();
    }
}
