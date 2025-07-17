namespace LogService.Application.Features.Logs.Commands;

using LogService.Application.Common.Results;
using LogService.SharedKernel.Constants;

using MediatR;

public class ClearCacheByRegionCommand : IRequest<Result>
{
    public string IndexName { get; set; } = LogConstants.DataStreamName;
}
