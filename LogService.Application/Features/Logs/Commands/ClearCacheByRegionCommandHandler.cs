namespace LogService.Application.Features.Logs.Commands;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Requests;
using LogService.Application.Common.Results;

using MediatR;

public class ClearCacheByRegionCommandHandler(ICacheRegionSupport regionSupport)
    : IRequestHandler<ClearCacheByRegionCommand, Result>
{
    public async Task<Result> Handle(ClearCacheByRegionCommand request, CancellationToken cancellationToken)
    {
        var region = $"region:{request.IndexName}";

        await regionSupport.InvalidateRegionAsync(region);

        return Result.Success();
    }
}
