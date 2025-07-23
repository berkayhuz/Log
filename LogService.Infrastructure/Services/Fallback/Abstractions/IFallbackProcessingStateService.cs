namespace LogService.Infrastructure.Services.Fallback.Abstractions;

using LogService.Application.Options;

public interface IFallbackProcessingStateService
{
    FallbackProcessingRuntimeOptions Current { get; }
    void UpdateOptions(FallbackProcessingRuntimeOptions options);
}

