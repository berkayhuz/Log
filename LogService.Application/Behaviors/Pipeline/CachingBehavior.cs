namespace LogService.Application.Behaviors.Pipeline;

using System;
using System.Threading;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Caching;
using LogService.Application.Abstractions.Requests;
using LogService.Application.Common.Results;
using LogService.SharedKernel.Helpers;

using MediatR;

using Microsoft.Extensions.Logging;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICachableRequest
        where TResponse : Result
{
    private readonly ICacheService _cache;
    private readonly ICacheRegionSupport _regionSupport;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        ICacheService cache,
        ICacheRegionSupport regionSupport,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _regionSupport = regionSupport;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var key = request.CacheKey;
        var expiration = request.Expiration;

        if (ShouldBypassCache(request, key, expiration))
        {
            _logger.LogDebug("Cache bypassed for {Request}", typeof(TRequest).Name);
            return await next();
        }

        var cached = await TryCatch.ExecuteAsync<TResponse?>(
            tryFunc: () => _cache.GetAsync<TResponse>(key),
            catchFunc: ex =>
            {
                _logger.LogError(ex, "Error retrieving cache for key {Key}", key);
                return Task.FromResult<TResponse?>(null);
            },
            logger: _logger,
            context: $"CachingBehavior.Get<{typeof(TRequest).Name}>"
        );

        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {Request} with key {Key}", typeof(TRequest).Name, key);
            return cached;
        }

        var response = await next();

        if (!response.IsFailure && expiration.HasValue)
        {
            await TryCatch.ExecuteAsync(
                tryFunc: async () =>
                {
                    await _cache.SetAsync(key, response, expiration.Value);
                    _logger.LogDebug("Cache set for {Request} with key {Key}", typeof(TRequest).Name, key);

                    if (!string.IsNullOrWhiteSpace(request.CacheRegion))
                    {
                        await _regionSupport.AddKeyToRegionAsync(request.CacheRegion, key);
                        _logger.LogDebug("Key {Key} added to region {Region}", key, request.CacheRegion);
                    }
                },
                logger: _logger,
                context: $"CachingBehavior.Set<{typeof(TRequest).Name}>"
            );
        }

        return response;
    }

    private static bool ShouldBypassCache(TRequest request, string key, TimeSpan? expiration)
    {
        return string.IsNullOrWhiteSpace(key)
            || expiration is null
            || (request is ICacheBypassableRequest bypass && bypass.BypassCache);
    }
}
