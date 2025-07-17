namespace LogService.Application.Abstractions.Fallback;

using LogService.Application.Options;

public interface IFallbackProcessingStateService
{
    FallbackProcessingRuntimeOptions Current { get; }
    void UpdateOptions(FallbackProcessingRuntimeOptions options);
}
