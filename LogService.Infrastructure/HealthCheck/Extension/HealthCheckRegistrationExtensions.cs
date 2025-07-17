namespace LogService.Infrastructure.HealthCheck.Extension;
using System;
using System.Linq;
using System.Reflection;

using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class HealthCheckRegistrationExtensions
{
    public static IHealthChecksBuilder AddAllAnnotatedHealthChecks(this IServiceCollection services)
    {
        var builder = services.AddHealthChecks();

        var healthCheckTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IHealthCheck).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

        foreach (var type in healthCheckTypes)
        {
            var nameAttr = type.GetCustomAttribute<NameAttribute>();
            if (nameAttr is null)
                continue;

            var tagsAttr = type.GetCustomAttribute<TagsAttribute>();

            builder.Add(new HealthCheckRegistration(
                name: nameAttr.Name,
                factory: sp => ActivatorUtilities.CreateInstance(sp, type) as IHealthCheck
                            ?? throw new InvalidOperationException($"Could not create {type.Name}"),
                failureStatus: HealthStatus.Unhealthy,
                tags: tagsAttr?.Tags
            ));
        }

        return builder;
    }

}
