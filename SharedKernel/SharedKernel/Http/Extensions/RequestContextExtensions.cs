namespace SharedKernel.Http.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Http.Abstractions;
using SharedKernel.Http.Middlewares;
using SharedKernel.Http.Services;

public static class RequestContextExtensions
{
    public static IServiceCollection AddRequestContext(this IServiceCollection services)
    {
        services.AddScoped<IRequestContextService, RequestContextService>();
        return services;
    }

    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestContextMiddleware>();
    }
}
