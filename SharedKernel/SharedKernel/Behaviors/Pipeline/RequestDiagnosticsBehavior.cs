namespace SharedKernel.Behaviors.Pipeline;

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using SharedKernel.Common.Results.Abstractions;

// 👇 Buradaki alias, IResult çakışmasını çözüyor
using SharedKernelResult = SharedKernel.Common.Results.Abstractions.IResult;

public class RequestDiagnosticsBehavior<TRequest, TResponse>(
    IHttpContextAccessor httpContextAccessor,
    ILogger<RequestDiagnosticsBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : SharedKernelResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var http = httpContextAccessor.HttpContext;

        var traceId = Activity.Current?.TraceId.ToString()
                      ?? http?.TraceIdentifier
                      ?? Guid.NewGuid().ToString();

        var ip = http?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        var path = http?.Request?.Path.ToString();

        logger.LogInformation(
            "→ Handling {RequestName} | TraceId={TraceId} | IP={Ip} | Path={Path}",
            typeof(TRequest).Name, traceId, ip, path
        );

        TResponse response;

        try
        {
            response = await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Unhandled exception during {RequestName}", typeof(TRequest).Name);
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw; // Derleyici için
        }

        if (response is ITraceableResult traceable)
        {
            traceable.TraceId = traceId;
            traceable.IpAddress = ip;
        }

        logger.LogInformation(
            "✔️  Handled {RequestName} | TraceId={TraceId} | Success={IsSuccess}",
            typeof(TRequest).Name, traceId, response.IsSuccess
        );

        return response;
    }
}
