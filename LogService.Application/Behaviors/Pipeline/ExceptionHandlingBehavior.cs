namespace LogService.Application.Behaviors.Pipeline;

using System.Net;

using LogService.Application.Common.Exception;
using LogService.Application.Common.Results;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Helpers;

using MediatR;

public class ExceptionHandlingBehavior<TRequest, TResponse>(
    IResultFactory<TResponse> resultFactory)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return await TryCatch.ExecuteAsync<TResponse>(
            tryFunc: () => next(),
            catchFunc: ex =>
            {
                var details = ExceptionHandler.Handle(ex);

                var severity = details.StatusCode switch
                {
                    HttpStatusCode.BadRequest => LogSeverityCode.Warning,
                    HttpStatusCode.InternalServerError => LogSeverityCode.Error,
                    HttpStatusCode.Forbidden => LogSeverityCode.Warning,
                    HttpStatusCode.NotFound => LogSeverityCode.Warning,
                    _ => LogSeverityCode.Fatal
                };

                var failure = resultFactory.CreateFailure(details.Errors ?? new[] { details.Message });
                return Task.FromResult(failure);
            },
            logger: null,
            context: $"ExceptionHandlingBehavior<{typeof(TRequest).Name}>"
        );
    }
}
