namespace LogService.Infrastructure.Services.Fallback.Writers;
using LogService.Domain.DTOs;
using LogService.Infrastructure.Services.Fallback.Abstractions;
using LogService.Infrastructure.Services.Logging.Abstractions;

using Polly;
using Polly.Wrap;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;

public class ResilientLogWriter : IResilientLogWriter
{
    private readonly ILogEntryWriteService _innerWriter;
    private readonly IFallbackLogWriter _fallbackWriter;
    private readonly AsyncPolicyWrap<Result> _resiliencePolicy;

    public ResilientLogWriter(
        ILogEntryWriteService innerWriter,
        IFallbackLogWriter fallbackWriter)
    {
        _innerWriter = innerWriter;
        _fallbackWriter = fallbackWriter;

        var retryPolicy = Policy<Result>
            .Handle<Exception>()
            .OrResult(r => r.IsFailure)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(300 * attempt)
            );

        var circuitBreakerPolicy = Policy<Result>
            .Handle<Exception>()
            .OrResult(r => r.IsFailure)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (_, _) => { },
                onReset: () => { }
            );

        var fallbackPolicy = Policy<Result>
            .Handle<Exception>()
            .OrResult(r => r.IsFailure)
            .FallbackAsync(
                fallbackAction: _ =>
                {
                    return Task.FromResult(
                        Result.Failure("Fallback'a düşüldü.")
                            .WithErrorType(ErrorType.Infrastructure)
                            .WithErrorCode(ErrorCode.DatabaseWriteFailed)
                            .WithStatusCode(StatusCodes.ServiceUnavailable)
                    );
                });

        _resiliencePolicy = Policy.WrapAsync(fallbackPolicy, retryPolicy, circuitBreakerPolicy);
    }

    public async Task<Result> WriteWithRetryAsync(LogEntryDto model, CancellationToken cancellationToken = default)
    {
        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            var result = await _innerWriter.WriteToElasticAsync(model);

            if (result.IsFailure)
                await _fallbackWriter.WriteAsync(model);

            return result;
        });
    }
}
