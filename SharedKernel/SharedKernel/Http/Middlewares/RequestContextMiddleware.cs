namespace SharedKernel.Http.Middlewares;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using SharedKernel.Http.Abstractions;

public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContextService requestContext)
    {
        var ip = context.Request.Headers["X-Request-IP"].FirstOrDefault()
                 ?? context.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";

        var device = context.Request.Headers["X-Request-Device"].FirstOrDefault()
                    ?? context.Request.Headers["User-Agent"].FirstOrDefault()
                    ?? "unknown";

        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();

        requestContext.SetContext(ip, device, correlationId);

        await _next(context);
    }
}
