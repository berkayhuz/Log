namespace SharedKernel.Common.Results;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using SharedKernel.Common.Results.Abstractions;
using SharedKernel.Common.Results.Extensions;
using SharedKernel.Common.Results.Json;
using SharedKernel.Common.Results.Objects;

public abstract record Result(bool IsSuccess, IReadOnlyList<string> Errors) : IResult
{
    public bool IsFailure => !IsSuccess;

    public List<string> ErrorCodes { get; init; } = new();
    public List<string> ErrorKeys { get; init; } = new();
    public Dictionary<string, object>? Metadata { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public int? StatusCode { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? UserId { get; private set; }
    public string? TenantId { get; private set; }
    public ErrorType ErrorType { get; private set; } = ErrorType.None;
    public ErrorLevel ErrorLevel { get; private set; } = ErrorLevel.Error;
    public List<ErrorCode> ErrorCodeEnums { get; init; } = new();

    public Result WithErrorCode(ErrorCode code)
    {
        if (code != ErrorCode.None && !ErrorCodeEnums.Contains(code))
            ErrorCodeEnums.Add(code);
        return this;
    }

    public Result WithErrorLevel(ErrorLevel level)
    {
        ErrorLevel = level;
        return this;
    }

    [JsonIgnore]
    public Exception? Exception { get; private set; }

    public static Result Success() => Result<Unit>.Success(Unit.Value);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result Failure(params string[] errors) => Result<Unit>.Failure(errors);
    public static Result Failure(IEnumerable<string> errors) => Result<Unit>.Failure(errors);

    public Result WithErrorCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(code)) ErrorCodes.Add(code);
        return this;
    }

    public Result WithErrorKey(string key)
    {
        if (!string.IsNullOrWhiteSpace(key)) ErrorKeys.Add(key);
        return this;
    }

    public Result WithMetadata(string key, object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Metadata ??= new();
        Metadata[key] = value;
        return this;
    }

    public Result WithException(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        Exception = ex;
        Metadata ??= new();
        Metadata["Exception"] = ex.ToString();
        return this;
    }

    public Result WithFieldError(string field, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Metadata ??= new();
        if (!Metadata.TryGetValue("FieldErrors", out var obj) || obj is not List<(string, string)> list)
        {
            list = new();
            Metadata["FieldErrors"] = list;
        }

        list.Add((field, message));
        return this;
    }

    public Result WithStatusCode(int statusCode)
    {
        StatusCode = statusCode;
        return this;
    }

    public Result WithCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }

    public Result WithUser(string userId)
    {
        UserId = userId;
        return this;
    }

    public Result WithTenant(string tenantId)
    {
        TenantId = tenantId;
        return this;
    }

    public Result WithErrorType(ErrorType type)
    {
        ErrorType = type;
        return this;
    }

    public bool IsUnauthorized => StatusCode == StatusCodes.Unauthorized;
    public bool IsNotFound => StatusCode == StatusCodes.NotFound;

    public bool IsValidationFailure => ErrorType == ErrorType.Validation;

    public ProblemDetails ToProblemDetails(int? statusCode = null)
    {
        return new ProblemDetails
        {
            Title = IsSuccess ? "Success" : "Failure",
            Status = statusCode ?? StatusCode ?? (IsSuccess ? StatusCodes.Ok : StatusCodes.BadRequest),
            Detail = Errors.Any() ? string.Join("; ", Errors) : null,
            Extensions =
        {
            ["ErrorCodes"] = ErrorCodes,
            ["ErrorCodeEnums"] = ErrorCodeEnums.Select(x => x.ToString()).ToList(),
            ["ErrorLevel"] = ErrorLevel.ToString(),
            ["ErrorKeys"] = ErrorKeys,
            ["ErrorType"] = ErrorType.ToString(),
            ["ErrorTypeDisplay"] = ErrorType.GetDisplayName(),
            ["CorrelationId"] = CorrelationId,
            ["UserId"] = UserId,
            ["TenantId"] = TenantId,
            ["Metadata"] = Metadata
        }
        };
    }

    public IActionResult ToActionResult()
    {
        if (IsSuccess)
            return new OkObjectResult(this);

        var status = StatusCode
            ?? (IsUnauthorized ? StatusCodes.Unauthorized
            : IsNotFound ? StatusCodes.NotFound
            : IsValidationFailure ? StatusCodes.ValidationFailed
            : StatusCodes.BadRequest);

        return new ObjectResult(ToProblemDetails(status)) { StatusCode = status };
    }


    public TResult Match<TResult>(Func<TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(Errors);
    }

    public override string ToString()
    {
        var baseStatus = IsSuccess ? "Success" : $"Failure: {string.Join(", ", Errors)}";
        var codes = ErrorCodes.Count > 0 ? $" | Codes: {string.Join(", ", ErrorCodes)}" : string.Empty;
        return $"[Result] {baseStatus}{codes} | At: {CreatedAt:u}";
    }

    public static async Task<Result> Try(
       Func<Task<Result>> func,
       ILogger? logger = null,
       string? context = null)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unhandled exception in {Context}", context ?? func.Method.Name);

            return Failure("An unexpected error occurred.")
                .WithException(ex)
                .WithErrorType(ErrorType.Unexpected);
        }
    }

    public static Result Combine(IEnumerable<Result> results)
    {
        var resultList = results.ToList();
        if (resultList.All(r => r.IsSuccess))
            return Success();

        var allErrors = resultList.Where(r => r.IsFailure)
                                   .SelectMany(r => r.Errors)
                                   .ToList();

        return Failure(allErrors);
    }

    public static Result<T[]> Combine<T>(IEnumerable<Result<T>> results)
    {
        var resultList = results.ToList();
        if (resultList.All(r => r.IsSuccess))
        {
            var values = resultList.Select(r => r.Value!).ToArray();
            return Success(values);
        }

        var allErrors = resultList.Where(r => r.IsFailure)
                                   .SelectMany(r => r.Errors)
                                   .ToList();

        return Result<T[]>.Failure(allErrors);
    }

    public static IReadOnlyList<string> AggregateErrors(IEnumerable<IResult> results)
    {
        return results.Where(r => r.IsFailure)
                      .SelectMany(r => r.Errors)
                      .ToList();
    }

    public static Result<TAccumulate> Fold<T, TAccumulate>(
    IEnumerable<Result<T>> results,
    TAccumulate seed,
    Func<TAccumulate, T, TAccumulate> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var value = seed;
        var errors = new List<string>();

        foreach (var result in results)
        {
            if (result.IsSuccess)
            {
                value = func(value, result.Value!);
            }
            else
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0 ? Result<TAccumulate>.Success(value) : Result<TAccumulate>.Failure(errors);
    }


    public static async Task<Result<T>> Try<T>(
        Func<Task<Result<T>>> func,
        ILogger? logger = null,
        string? context = null)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unhandled exception in {Context}", context ?? func.Method.Name);

            return Result<T>.Failure("An unexpected error occurred.")
                .WithException(ex)
                .WithErrorType(ErrorType.Unexpected);
        }
    }
}

[JsonConverter(typeof(ResultJsonConverterFactory))]
public sealed record Result<T>(bool IsSuccess, T? Value, IReadOnlyList<string> Errors)
    : Result(IsSuccess, Errors), IResult<T>
{
    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(true, value, Array.Empty<string>());
    }

    public static new Result<T> Failure(params string[] errors) => new(false, default, errors ?? Array.Empty<string>());
    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors?.ToArray() ?? Array.Empty<string>());

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = IsSuccess ? Value : default;
        return IsSuccess;
    }

    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? Result<TOut>.Success(map(Value!)) : Result<TOut>.Failure(Errors);
    }

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess ? bind(Value!) : Result<TOut>.Failure(Errors);
    }

    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? Result<TOut>.Success(await map(Value!)) : Result<TOut>.Failure(Errors);
    }

    public async ValueTask<Result<TOut>> MapAsync<TOut>(Func<T, ValueTask<TOut>> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? Result<TOut>.Success(await map(Value!)) : Result<TOut>.Failure(Errors);
    }

    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess ? await bind(Value!) : Result<TOut>.Failure(Errors);
    }

    public new Result<T> WithErrorCode(ErrorCode code)
    {
        base.WithErrorCode(code);
        return this;
    }

    public new Result<T> WithErrorLevel(ErrorLevel level)
    {
        base.WithErrorLevel(level);
        return this;
    }


    public async ValueTask<Result<TOut>> BindAsync<TOut>(Func<T, ValueTask<Result<TOut>>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return IsSuccess ? await bind(Value!) : Result<TOut>.Failure(Errors);
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value!) : onFailure(Errors);
    }

    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess)
        {
            ArgumentNullException.ThrowIfNull(action);
            action(Value!);
        }
        return this;
    }

    public Result<T> Ensure(Func<T, bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorMessage);
        return !IsSuccess || predicate(Value!) ? this : Failure(errorMessage);
    }

    public void Deconstruct(out bool isSuccess, out T? value, out IReadOnlyList<string> errors)
    {
        isSuccess = IsSuccess;
        value = Value;
        errors = Errors;
    }

    public new Result<T> WithErrorCode(string code)
    {
        base.WithErrorCode(code);
        return this;
    }

    public new Result<T> WithErrorKey(string key)
    {
        base.WithErrorKey(key);
        return this;
    }

    public new Result<T> WithMetadata(string key, object value)
    {
        base.WithMetadata(key, value);
        return this;
    }

    public new Result<T> WithException(Exception ex)
    {
        base.WithException(ex);
        return this;
    }

    public new Result<T> WithFieldError(string field, string message)
    {
        base.WithFieldError(field, message);
        return this;
    }

    public new Result<T> WithStatusCode(int statusCode)
    {
        base.WithStatusCode(statusCode);
        return this;
    }

    public new Result<T> WithCorrelationId(string correlationId)
    {
        base.WithCorrelationId(correlationId);
        return this;
    }

    public new Result<T> WithUser(string userId)
    {
        base.WithUser(userId);
        return this;
    }

    public new Result<T> WithTenant(string tenantId)
    {
        base.WithTenant(tenantId);
        return this;
    }

    public new Result<T> WithErrorType(ErrorType type)
    {
        base.WithErrorType(type);
        return this;
    }

    public static implicit operator Result<T>(T value) => Success(value);

    public static explicit operator T(Result<T> result) => result.IsSuccess
        ? result.Value!
        : throw new InvalidOperationException($"Cannot cast failure result to value. Errors: {string.Join(", ", result.Errors)}");
}

