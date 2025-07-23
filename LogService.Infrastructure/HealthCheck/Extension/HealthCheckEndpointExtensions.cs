namespace LogService.Infrastructure.HealthCheck.Extension;
using System.Linq;
using System.Reflection;

using global::HealthChecks.UI.Client;

using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class HealthCheckEndpointExtensions
{
    public static void MapAllHealthCheckEndpoints(this WebApplication app)
    {
        var healthCheckTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IHealthCheck).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in healthCheckTypes)
        {
            var nameAttr = type.GetCustomAttribute<NameAttribute>();
            if (nameAttr is null) continue;

            var path = $"/health/{nameAttr.Name.Replace('_', '-')}";

            app.MapHealthChecks(path, new HealthCheckOptions
            {
                Predicate = check => check.Name == nameAttr.Name,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        }

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
    }
}
