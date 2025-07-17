namespace LogService.Application.Behaviors.Pipeline;

using System.Net;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Exception;
using LogService.Application.Common.Results;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Keys;

using MediatR;

public class ExceptionHandlingBehavior<TRequest, TResponse>(
    IResultFactory<TResponse> resultFactory,
    ILogServiceLogger logLogger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        const string className = nameof(ExceptionHandlingBehavior<TRequest, TResponse>);
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var details = ExceptionHandler.Handle(ex);

            var severity = details.StatusCode switch
            {
                HttpStatusCode.BadRequest => LogStage.Warning,
                HttpStatusCode.InternalServerError => LogStage.Error,
                HttpStatusCode.Forbidden => LogStage.Warning,
                HttpStatusCode.NotFound => LogStage.Warning,
                _ => LogStage.Fatal
            };

            await logLogger.LogAsync(
                severity,
                LogMessageDefaults.Messages[LogMessageKeys.Exception_UnhandledRequest]
                    .Replace("{Request}", typeof(TRequest).Name)
                    .Replace("{Message}", details.Message),
                ex
            );

            var failure = resultFactory.CreateFailure(details.Errors ?? new[] { details.Message });

            return failure;
        }
    }
}
