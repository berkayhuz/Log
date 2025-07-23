namespace SharedKernel.Common.Results.Abstractions;
using System.Collections.Generic;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure => !IsSuccess;
    IReadOnlyList<string> Errors { get; }
}

public interface IResult<out T> : IResult
{
    T? Value { get; }
}
