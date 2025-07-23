namespace LogService.Infrastructure.Services.Elastic;
using LogService.Infrastructure.Services.Elastic.Abstractions;
using LogService.Infrastructure.Services.Elastic.Clients;
using LogService.Infrastructure.Services.Elastic.Health;
using LogService.Infrastructure.Services.Elastic.Indexing;

using Microsoft.Extensions.DependencyInjection;

public static class ElasticModule
{
    public static IServiceCollection AddElasticInfrastructure(this IServiceCollection services)
    {
        return services
            .AddScoped<IElasticLogClient, ElasticLogClient>()
            .AddScoped<IElasticIndexService, ElasticIndexService>()
            .AddScoped<IElasticHealthService, ElasticHealthService>();
    }
}

