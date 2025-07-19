namespace LogService.Application.Behaviors.Pipeline;
using System.Diagnostics;
using System.Threading.Tasks;

using LogService.Application.Common.Result;
using LogService.Application.Common.Results;
using LogService.SharedKernel.Helpers;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class RequestDiagnosticsBehavior<TRequest, TResponse>(
    IHttpContextAccessor httpContextAccessor,
    ILogger<RequestDiagnosticsBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var http = httpContextAccessor.HttpContext;
        var traceId = Activity.Current?.TraceId.ToString()
                      ?? http?.TraceIdentifier
                      ?? "no-trace";

        var ip = http?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        logger.LogInformation(
            "Handling {Request} | Trace={TraceId} | IP={Ip}",
            typeof(TRequest).Name, traceId, ip);

        var response = await TryCatch.ExecuteAsync<TResponse>(
            tryFunc: () => next(),
            catchFunc: ex =>
            {
                logger.LogError(ex, "Unhandled exception during {Request}", typeof(TRequest).Name);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
                return Task.FromException<TResponse>(ex);
            },
            logger: logger,
            context: $"RequestDiagnosticsBehavior<{typeof(TRequest).Name}>"
        );

        if (response is ITraceableResult tr)
        {
            tr.TraceId = traceId;
            tr.IpAddress = ip;
        }

        return response;
    }
}
