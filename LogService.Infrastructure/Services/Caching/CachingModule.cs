namespace LogService.Infrastructure.Services.Caching;

using LogService.Application.Abstractions.Caching;
using LogService.Application.Abstractions.Requests;
using LogService.Infrastructure.Services.Caching.Abstractions;
using LogService.Infrastructure.Services.Caching.Extensions;
using LogService.Infrastructure.Services.Caching.Redis;

using Microsoft.Extensions.DependencyInjection;

public static class CachingModule
{
    public static IServiceCollection AddCaching(this IServiceCollection services)
    {
        return services
            .AddSingleton<ICacheService, RedisCacheService>()
            .AddSingleton<ICacheRegionSupport, RedisCacheRegionSupport>()
            .AddSingleton<IStringDistributedCache, StringDistributedCache>();
    }
}
