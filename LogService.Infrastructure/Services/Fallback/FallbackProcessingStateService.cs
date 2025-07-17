namespace LogService.Infrastructure.Services.Fallback;

using LogService.Application.Abstractions.Fallback;
using LogService.Application.Options;
public class FallbackProcessingStateService : IFallbackProcessingStateService
{
    private readonly FallbackProcessingRuntimeOptions _current = new();

    public FallbackProcessingRuntimeOptions Current
    {
        get
        {
            const string className = nameof(FallbackProcessingStateService);

            return _current;
        }
    }

    public void UpdateOptions(FallbackProcessingRuntimeOptions options)
    {
        const string className = nameof(FallbackProcessingStateService);

        _current.EnableResilient = options.EnableResilient;
        _current.EnableDirect = options.EnableDirect;
        _current.EnableRetry = options.EnableRetry;
        _current.IntervalSeconds = options.IntervalSeconds;
    }
}
