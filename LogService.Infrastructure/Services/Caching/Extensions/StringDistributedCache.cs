namespace LogService.Infrastructure.Services.Caching.Extensions;

using System;
using System.Threading;
using System.Threading.Tasks;

using LogService.Infrastructure.Services.Caching.Abstractions;

using Microsoft.Extensions.Caching.Distributed;

public class StringDistributedCache : IStringDistributedCache
{
    private readonly IDistributedCache _cache;

    public StringDistributedCache(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var bytes = await _cache.GetAsync(key, token);
        return bytes is null ? null : System.Text.Encoding.UTF8.GetString(bytes);
    }

    public async Task SetStringAsync(string key, string value, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
            return;

        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        await _cache.SetAsync(key, bytes, options ?? new DistributedCacheEntryOptions(), token);
    }



    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        await _cache.RemoveAsync(key, token);
    }
}
