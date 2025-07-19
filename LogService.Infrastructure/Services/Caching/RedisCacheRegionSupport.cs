namespace LogService.Infrastructure.Services.Caching;
using System;
using System.Linq;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Requests;
using LogService.SharedKernel.Constants;
using LogService.SharedKernel.Helpers;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

public class RedisCacheRegionSupport : ICacheRegionSupport
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheRegionSupport> _logger;

    public RedisCacheRegionSupport(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheRegionSupport> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private RedisKey GetRegionKey(string region)
        => (RedisKey)(CacheConstants.RegionSetPrefix + region);

    public async Task AddKeyToRegionAsync(string region, string key)
    {
        await TryCatch.ExecuteAsync(
            tryFunc: async () =>
            {
                var db = _redis.GetDatabase();
                await db.SetAddAsync(GetRegionKey(region), key);
            },
            logger: _logger,
            context: $"RedisCacheRegionSupport.AddKeyToRegionAsync({region})"
        );
    }

    public async Task InvalidateRegionAsync(string region)
    {
        await TryCatch.ExecuteAsync(
            tryFunc: async () =>
            {
                var db = _redis.GetDatabase();
                var regionKey = GetRegionKey(region);

                var members = await db.SetMembersAsync(regionKey);
                if (members.Length > 0)
                {
                    var redisKeys = members.Select(v => (RedisKey)v.ToString()).ToArray();
                    await db.KeyDeleteAsync(redisKeys);
                }

                await db.KeyDeleteAsync(regionKey);
            },
            logger: _logger,
            context: $"RedisCacheRegionSupport.InvalidateRegionAsync({region})"
        );
    }
}
