namespace LogService.Application.Behaviors.Pipeline;

using FluentValidation;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Results;
using LogService.SharedKernel.Helpers;

using MediatR;

using Microsoft.Extensions.Logging;

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    IResultFactory<TResponse> resultFactory,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        return await TryCatch.ExecuteAsync<TResponse>(
            tryFunc: async () =>
            {
                var context = new ValidationContext<TRequest>(request);

                var validationResults = await Task.WhenAll(
                    validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(e => e is not null)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                if (failures.Any())
                {
                    return resultFactory.CreateFailure(failures);
                }

                return await next();
            },
            catchFunc: ex =>
            {
                logger.LogError(ex, "Validation sırasında beklenmeyen hata oluştu: {Request}", typeof(TRequest).Name);
                return Task.FromResult(
                    resultFactory.CreateFailure(new[] { "Validation pipeline error: " + ex.Message })
                );
            },
            logger: logger,
            context: $"ValidationBehavior<{typeof(TRequest).Name}>"
        );
    }
}
