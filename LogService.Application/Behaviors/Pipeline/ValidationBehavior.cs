namespace LogService.Application.Behaviors.Pipeline;

using FluentValidation;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Common.Results;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Keys;

using MediatR;

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    IResultFactory<TResponse> resultFactory,
    ILogServiceLogger logLogger)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        const string className = nameof(ValidationBehavior<TRequest, TResponse>);

        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .Select(e => e.ErrorMessage)
            .ToList();

        if (failures.Any())
        {

            await logLogger.LogAsync(
                LogStage.Warning,
                LogMessageDefaults.Messages[LogMessageKeys.Exception_ValidationFailed]
                    .Replace("{Request}", typeof(TRequest).Name)
                    .Replace("{Failures}", string.Join("; ", failures)));

            return resultFactory.CreateFailure(failures);
        }

        return await next();
    }
}
