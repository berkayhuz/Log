using Polly;
using Polly.Retry;
using LogService.Infrastructure.Services.Logging.Abstractions;

namespace LogService.Infrastructure.Services.Fallback.RetryPolicies;

public static class PollyPolicies
{
    public static AsyncRetryPolicy<IElasticResponseWrapper> RetryElasticPolicy =>
        Policy
            .HandleResult<IElasticResponseWrapper>(r => !r.IsValidResponse)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(300 * attempt),
                onRetry: (result, timespan, retryCount, context) =>
                {
                    // Optionally log or trace here if needed
                    // Example:
                    // Console.WriteLine($"Elastic retry #{retryCount}, will wait {timespan.TotalMilliseconds}ms");
                });
}
