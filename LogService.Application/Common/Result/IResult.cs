namespace LogService.Application.Common.Results;
using System.Collections.Generic;

public interface IResult
{
    bool IsSuccess { get; }
    IReadOnlyList<string> Errors { get; }
}

public interface IResult<T> : IResult
{
    T Value { get; }
}
