namespace LogService.API.Controllers;

#region LogsController/Usings
using LogService.Application.Abstractions.Requests;
using LogService.Application.Features.Logs.Queries.QueryLogsFlexible;
using LogService.Infrastructure.Services.Elastic.Abstractions;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;
using SharedKernel.Filters;
#endregion

[Authorize]
[ApiController]
[Route("api/logs/")]
[ServiceFilter(typeof(RequireMatchingRoleHeaderFilter))]
public class LogsController(
    IMediator mediator,
    IElasticIndexService indexService,
    ICacheRegionSupport cacheRegionSupport)
    : ControllerBase
{
    [HttpPost("flexible")]
    public async Task<IActionResult> FlexibleLogQuery([FromBody] QueryLogsFlexible query)
    {
        var result = await mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("indices")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetIndices(CancellationToken cancellationToken)
    {
        var indices = await indexService.GetIndexNamesAsync(cancellationToken);
        return Result.Success(indices).ToActionResult();
    }

    [HttpDelete("{indexName}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ClearIndexCache(
    [FromRoute] string indexName,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(indexName))
        {
            return Result.Failure("Index adı boş olamaz.")
                .WithStatusCode(StatusCodes.BadRequest)
                .WithErrorType(ErrorType.Validation)
                .ToActionResult();
        }

        try
        {
            var result = await Result.Try(async () =>
            {
                var regionKey = $"region:{indexName}";
                await cacheRegionSupport.InvalidateRegionAsync(regionKey);
                return Result.Success()
                    .WithMetadata("Region", regionKey);
            });

            return result.ToActionResult();
        }
        catch (Exception ex)
        {
            return Result.Failure("Beklenmeyen bir hata oluştu.")
                .WithException(ex)
                .WithErrorType(ErrorType.Unexpected)
                .WithStatusCode(StatusCodes.InternalServerError)
                .ToActionResult();
        }
    }
}
