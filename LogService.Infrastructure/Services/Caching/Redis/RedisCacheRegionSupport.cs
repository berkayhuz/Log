namespace LogService.Infrastructure.Services.Caching.Redis;

using System;
using System.Linq;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Requests;
using LogService.Domain.Constants;

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
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static RedisKey GetRegionKey(string region)
        => (RedisKey)(CacheConstants.RegionSetPrefix + region);

    public async Task AddKeyToRegionAsync(string region, string key)
    {
        if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            var db = _redis.GetDatabase();
            await db.SetAddAsync(GetRegionKey(region), key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis AddKeyToRegionAsync işlemi başarısız: {Region}", region);
        }
    }

    public async Task InvalidateRegionAsync(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
            return;

        try
        {
            var db = _redis.GetDatabase();
            var regionKey = GetRegionKey(region);

            RedisValue[] members = await db.SetMembersAsync(regionKey);
            if (members?.Length > 0)
            {
                RedisKey[] redisKeys = members
                    .Where(v => !v.IsNullOrEmpty)
                    .Select(v => (RedisKey)v.ToString())
                    .ToArray();

                if (redisKeys.Length > 0)
                    await db.KeyDeleteAsync(redisKeys);
            }

            await db.KeyDeleteAsync(regionKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis InvalidateRegionAsync işlemi başarısız: {Region}", region);
        }
    }
}
