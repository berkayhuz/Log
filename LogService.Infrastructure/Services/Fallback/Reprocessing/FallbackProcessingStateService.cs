namespace LogService.Infrastructure.Services.Fallback.Reprocessing;

using LogService.Application.Options;
using LogService.Infrastructure.Services.Fallback.Abstractions;

public class FallbackProcessingStateService : IFallbackProcessingStateService
{
    private readonly FallbackProcessingRuntimeOptions _state = new();
    private readonly object _syncRoot = new();

    public FallbackProcessingRuntimeOptions Current
    {
        get
        {
            lock (_syncRoot)
            {
                return _state;
            }
        }
    }

    public void UpdateOptions(FallbackProcessingRuntimeOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        lock (_syncRoot)
        {
            _state.EnableResilient = options.EnableResilient;
            _state.EnableDirect = options.EnableDirect;
            _state.EnableRetry = options.EnableRetry;
            _state.IntervalSeconds = options.IntervalSeconds;
        }
    }
}
