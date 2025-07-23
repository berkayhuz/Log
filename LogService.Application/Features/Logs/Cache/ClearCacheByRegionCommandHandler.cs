namespace LogService.Application.Features.Logs.Cache;

using LogService.Application.Abstractions.Requests;
using LogService.Domain.Constants;

using MediatR;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;

public class ClearCacheByRegionCommandHandler(ICacheRegionSupport regionSupport)
    : IRequestHandler<ClearCacheByRegionCommand, Result>
{
    public async Task<Result> Handle(ClearCacheByRegionCommand request, CancellationToken cancellationToken)
    {
        var region = CacheConstants.RegionSetPrefix + request.IndexName;

        try
        {
            await regionSupport.InvalidateRegionAsync(region);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("Cache temizleme sırasında hata oluştu.")
                .WithException(ex)
                .WithErrorCode(ErrorCode.CacheUnavailable)
                .WithErrorType(ErrorType.Cache)
                .WithStatusCode(StatusCodes.InternalServerError);
        }
    }
}
