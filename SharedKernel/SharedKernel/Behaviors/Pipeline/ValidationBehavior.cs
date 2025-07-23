namespace SharedKernel.Behaviors.Pipeline;

using FluentValidation;

using MediatR;

using Microsoft.Extensions.Logging;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Abstractions;
using SharedKernel.Common.Results.Extensions;
using SharedKernel.Common.Results.Objects;

// Microsoft.AspNetCore.Http.IResult çakışmasından kaçınmak için alias
using SharedKernelResult = SharedKernel.Common.Results.Abstractions.IResult;

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : SharedKernelResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(e => !string.IsNullOrWhiteSpace(e?.ErrorMessage))
            .Select(e => e!.ErrorMessage)
            .Distinct()
            .ToList();

        if (failures.Count > 0)
        {
            logger.LogWarning(
                "Validation failed for {Request} with errors: {Errors}",
                typeof(TRequest).Name,
                string.Join(" | ", failures)
            );

            var failureResult = Result.Failure(failures)
                .WithValidationFailure(); // StatusCode = 461, ErrorType = Validation

            return CastToGenericResponse(failureResult);
        }

        return await next();
    }

    private static TResponse CastToGenericResponse(Result failure)
    {
        var genericType = typeof(TResponse);
        if (genericType.IsGenericType &&
            genericType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = genericType.GetGenericArguments()[0];

            var failureGeneric = typeof(Result<>).MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Failure), new[] { typeof(IEnumerable<string>) })!
                .Invoke(null, new object[] { failure.Errors });

            // Common metadata (status code, error type)
            typeof(Result<>).MakeGenericType(valueType)
                .GetMethod("WithValidationFailure")!
                .Invoke(failureGeneric, null);


            return (TResponse)failureGeneric!;
        }

        // Eğer non-generic Result dönen bir handler varsa (rare)
        return (TResponse)(object)failure;
    }
}
