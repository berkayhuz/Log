namespace LogService.Application.Behaviors.Pipeline;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Logging;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Keys;

using MediatR;

using Microsoft.AspNetCore.Http;

public class RequestDiagnosticsBehavior<TRequest, TResponse>(
    IHttpContextAccessor contextAccessor,
    ILogServiceLogger logLogger)
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogServiceLogger _logLogger = logLogger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        const string className = nameof(RequestDiagnosticsBehavior<TRequest, TResponse>);
        var requestName = typeof(TRequest).Name;
        var httpContext = contextAccessor.HttpContext;

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext?.TraceIdentifier ?? "no-trace";
        var userId = httpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var role = httpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? "unknown";
        var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        await _logLogger.LogAsync(
            LogStage.Debug,
            LogMessageDefaults.Messages[LogMessageKeys.Trace_Handling]
                .Replace("{Request}", requestName)
                .Replace("{TraceId}", traceId)
                .Replace("{UserId}", userId)
                .Replace("{Role}", role)
                .Replace("{IP}", ip));

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        await _logLogger.LogAsync(
            LogStage.Debug,
            LogMessageDefaults.Messages[LogMessageKeys.Trace_Handled]
                .Replace("{Request}", requestName)
                .Replace("{TraceId}", traceId)
                .Replace("{Elapsed}", stopwatch.ElapsedMilliseconds.ToString()));

        if (stopwatch.ElapsedMilliseconds > 500)
        {
            await _logLogger.LogAsync(
                LogStage.Warning,
                LogMessageDefaults.Messages[LogMessageKeys.Perf_RequestSlow]
                    .Replace("{Request}", requestName)
                    .Replace("{Elapsed}", stopwatch.ElapsedMilliseconds.ToString()));
        }

        return response;
    }
}
