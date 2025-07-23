namespace LogService.Infrastructure.Services.Fallback;
using LogService.Infrastructure.Services.Fallback.Abstractions;
using LogService.Infrastructure.Services.Fallback.Reprocessing;
using LogService.Infrastructure.Services.Fallback.Writers;

using Microsoft.Extensions.DependencyInjection;

public static class FallbackModule
{
    public static IServiceCollection AddResilientLogging(this IServiceCollection services)
    {
        return services
            .AddScoped<IFallbackLogWriter, FallbackLogWriter>()
            .AddScoped<IResilientLogWriter, ResilientLogWriter>()
            .AddSingleton<FallbackProcessingStateService>()
            .AddHostedService<FallbackLogReprocessingService>();
    }
}
