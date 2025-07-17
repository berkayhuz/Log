namespace LogService.Application.Common.Results;
using System.Collections.Generic;

public interface IResultFactory<TResponse>
{
    TResponse CreateFailure(IEnumerable<string> errors);
}
