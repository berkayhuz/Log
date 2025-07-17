namespace LogService.Application.Behaviors.Pipeline;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Caching;
using LogService.Application.Abstractions.Logging;
using LogService.Application.Abstractions.Requests;
using LogService.Application.Common.Results;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Keys;

using MediatR;

public class CachingBehavior<TRequest, TResponse>(
    ICacheService cache,
    ICacheRegionSupport regionSupport,
    ILogServiceLogger logLogger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachableRequest
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        const string className = nameof(CachingBehavior<TRequest, TResponse>);
        var key = request.CacheKey;
        var expiration = request.Expiration;

        if (ShouldBypassCache(request, key, expiration))
        {
            return await next();
        }

        var cached = await TryGetFromCache(key);
        if (cached != null)
        {
            return cached;
        }

        var response = await next();

        if (!response.IsFailure)
        {
            await cache.SetAsync(key, response, expiration.Value);

            if (request.CacheRegion is string regionName)
            {
                await regionSupport.AddKeyToRegionAsync(regionName, key);
            }

            await logLogger.LogAsync(LogStage.Debug,
                LogMessageDefaults.Messages[LogMessageKeys.Cache_SetWithExpiration]
                    .Replace("{key}", key)
                    .Replace("{Expiration}", expiration.Value.TotalSeconds.ToString("F0")));
        }

        return response;
    }

    private static bool ShouldBypassCache(TRequest request, string key, TimeSpan? expiration)
    {
        if (string.IsNullOrWhiteSpace(key) || expiration == null)
            return true;

        if (request is ICacheBypassableRequest bypassable && bypassable.BypassCache)
            return true;

        return false;
    }

    private async Task<TResponse?> TryGetFromCache(string key)
    {
        const string className = nameof(CachingBehavior<TRequest, TResponse>);
        try
        {
            var cached = await cache.GetAsync<TResponse>(key);
            if (cached != null)
            {
                await logLogger.LogAsync(LogStage.Debug,
                    LogMessageDefaults.Messages[LogMessageKeys.Cache_Hit].Replace("{key}", key));
            }
            else
            {
                await logLogger.LogAsync(LogStage.Debug,
                    LogMessageDefaults.Messages[LogMessageKeys.Cache_Miss].Replace("{key}", key));
            }

            return cached;
        }
        catch (Exception ex)
        {
            await logLogger.LogAsync(LogStage.Warning,
                LogMessageDefaults.Messages[LogMessageKeys.Cache_RetrievalFailed].Replace("{key}", key), ex);
            return null;
        }
    }
}
