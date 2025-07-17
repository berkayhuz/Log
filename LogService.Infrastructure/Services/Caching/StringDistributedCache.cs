namespace LogService.Infrastructure.Services.Caching;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Caching;

using Microsoft.Extensions.Caching.Distributed;

public class StringDistributedCache(IDistributedCache cache) : IStringDistributedCache
{
    public Task<string?> GetStringAsync(string key, CancellationToken token = default)
        => cache.GetStringAsync(key, token);

    public Task SetStringAsync(string key, string value, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
        => cache.SetStringAsync(key, value, options, token);

    public Task RemoveAsync(string key, CancellationToken token = default)
        => cache.RemoveAsync(key, token);
}

