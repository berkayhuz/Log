namespace LogService.Infrastructure.Services.Messages;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Messages;
using LogService.SharedKernel.Keys;

using Microsoft.Extensions.Caching.Distributed;

public class RedisLogMessageSeeder(IDistributedCache redis, ILogMessageProvider messageProvider) : ILogMessageSeeder
{
    public async Task SeedAsync()
    {
        foreach (var kvp in LogMessageDefaults.Messages)
        {
            await redis.SetStringAsync(kvp.Key, kvp.Value);
            await messageProvider.AddKeyAsync(kvp.Key);
        }
    }
}
