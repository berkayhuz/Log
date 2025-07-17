namespace LogService.Application.Common.Results;
using System.Collections.Generic;

public class ResultFactory : IResultFactory<Result>
{
    public Result CreateFailure(IEnumerable<string> errors) =>
        Result.Failure(errors);
}

public class ResultFactory<T> : IResultFactory<Result<T>>
{
    public Result<T> CreateFailure(IEnumerable<string> errors) =>
        Result<T>.Failure(errors);
}
