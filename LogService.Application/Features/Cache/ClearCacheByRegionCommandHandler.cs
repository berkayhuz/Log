namespace LogService.Application.Features.Cache;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Requests;
using LogService.Application.Common.Results;

using MediatR;

public class ClearCacheByRegionCommandHandler
        : IRequestHandler<ClearCacheByRegionCommand, Result>
{
    private readonly ICacheRegionSupport _regionSupport;

    public ClearCacheByRegionCommandHandler(ICacheRegionSupport regionSupport)
        => _regionSupport = regionSupport;

    public async Task<Result> Handle(
        ClearCacheByRegionCommand request,
        CancellationToken ct)
    {
        await _regionSupport.InvalidateRegionAsync(request.IndexName);
        return Result.Success();
    }
}
