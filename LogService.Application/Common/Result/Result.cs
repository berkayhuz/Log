namespace LogService.Application.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;

public abstract record Result(bool IsSuccess, IReadOnlyList<string> Errors) : IResult
{
    public bool IsFailure => !IsSuccess;

    public static Result Success()
        => new SimpleResult(true, Array.Empty<string>());

    public static Result Failure(params string[] errors)
        => new SimpleResult(false, errors ?? Array.Empty<string>());

    public static Result Failure(IEnumerable<string> errors)
        => new SimpleResult(false, errors?.ToArray() ?? Array.Empty<string>());

    private sealed record SimpleResult(bool IsSuccess, IReadOnlyList<string> Errors)
        : Result(IsSuccess, Errors);
}

public sealed record Result<T>(bool IsSuccess, T Value, IReadOnlyList<string> Errors)
    : Result(IsSuccess, Errors), IResult<T>
{
    public static Result<T> Success(T value)
        => new(true, value, Array.Empty<string>());

    public static new Result<T> Failure(params string[] errors)
        => new(false, default!, errors ?? Array.Empty<string>());

    public static new Result<T> Failure(IEnumerable<string> errors)
        => new(false, default!, errors?.ToArray() ?? Array.Empty<string>());
}
