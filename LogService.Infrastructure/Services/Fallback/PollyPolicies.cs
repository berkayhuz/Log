using Elastic.Clients.Elasticsearch;

using Polly;
using Polly.Retry;

namespace LogService.Infrastructure.Services.Fallback;

public static class PollyPolicies
{
    public static AsyncRetryPolicy<IndexResponse> RetryElasticPolicy =>
        Policy<IndexResponse>
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(300 * attempt)
            );
}
