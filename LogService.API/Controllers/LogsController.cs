namespace LogService.API.Controllers;

#region LogsController/Usings
using LogService.API.Filters;
using LogService.Application.Abstractions.Elastic;
using LogService.Application.Abstractions.Requests;
using LogService.Application.Features.Logs.Commands;
using LogService.Application.Features.Logs.Queries.QueryLogsFlexible;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#endregion

[Authorize]
[ApiController]
[Route("logs")]
[RequireMatchingRoleHeader]
public class LogsController(IMediator mediator, IElasticIndexService indexService) : ControllerBase
{
    [Authorize]
    [HttpPost("flexible")]
    public async Task<IActionResult> FlexibleLogQuery([FromBody] QueryLogsFlexible query)
    {
        var result = await mediator.Send(query);

        if (result.IsFailure) return BadRequest(result.Errors);

        return Ok(result.Value);
    }
    [HttpDelete("indices/{indexName}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ClearIndexCache(
    [FromRoute] string indexName,
    [FromServices] ICacheRegionSupport cacheRegionSupport,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(indexName))
            return BadRequest("Index adı boş olamaz.");

        var regionKey = $"region:{indexName}";
        await cacheRegionSupport.InvalidateRegionAsync(regionKey);

        return Ok(new { Message = $"Cache region cleared for {regionKey}" });
    }

    [HttpGet("indices")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetIndices(CancellationToken cancellationToken)
    {
        var indices = await indexService.GetIndexNamesAsync(cancellationToken);
        return Ok(indices);
    }
}
