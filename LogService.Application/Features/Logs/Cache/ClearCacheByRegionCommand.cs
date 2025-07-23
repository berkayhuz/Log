namespace LogService.Application.Features.Logs.Cache;

using LogService.Domain.Constants;

using MediatR;

using SharedKernel.Common.Results;

public class ClearCacheByRegionCommand : IRequest<Result>
{
    public string IndexName { get; set; } = LogConstants.DataStreamName;
}
