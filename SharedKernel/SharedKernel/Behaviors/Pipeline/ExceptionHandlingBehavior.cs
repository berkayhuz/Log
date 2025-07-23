namespace SharedKernel.Behaviors.Pipeline;

using MediatR;

using Microsoft.Extensions.Logging;

using SharedKernel.Common.Exceptions;
using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Extensions;

public class ExceptionHandlingBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in request {RequestName}", typeof(TRequest).Name);

            // 1. Exception map'inden `Result` al
            var baseResult = ExceptionHandler.Handle(ex);

            // 2. Eğer TResponse Result<T> ise buraya wrap edelim
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var result = CreateGenericFailureResult(baseResult);
                return (TResponse)result;
            }

            // 3. Değilse exception throwla
            throw new InvalidOperationException($"TResponse must be of type Result<T> to be handled.");
        }
    }

    private static object CreateGenericFailureResult(Result baseResult)
    {
        var valueType = typeof(TResponse).GetGenericArguments()[0];

        var failureGeneric = typeof(Result<>).MakeGenericType(valueType)
            .GetMethod(nameof(Result<object>.Failure), new[] { typeof(IEnumerable<string>) })!
            .Invoke(null, new object[] { baseResult.Errors })!;

        var enriched = typeof(ExceptionHandlingBehavior<TRequest, TResponse>)
            .GetMethod(nameof(ApplyCommonErrorProperties), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(valueType)
            .Invoke(null, new[] { failureGeneric, baseResult });

        return enriched!;
    }

    private static Result<T> ApplyCommonErrorProperties<T>(object failure, Result source)
    {
        return ((Result<T>)failure)
            .WithStatusCode(source.StatusCode ?? 500)
            .WithException(source.Exception!)
            .WithErrorCode(source.ErrorCodes.FirstOrDefault() ?? source.Exception?.GetType().Name ?? "Unknown")
            .WithErrorType(source.ErrorType)
            .WithErrorLevel(source.ErrorType.ToErrorLevel())
            .WithCorrelationId(source.CorrelationId ?? string.Empty)
            .WithUser(source.UserId ?? string.Empty)
            .WithTenant(source.TenantId ?? string.Empty);
    }
}
