namespace LogService.API.Controllers;

#region LogsController/Usings
using LogService.API.Filters;
using LogService.Application.Abstractions.Elastic;
using LogService.Application.Abstractions.Requests;
using LogService.Application.Features.Logs.Queries.QueryLogsFlexible;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Authorize]
    [HttpPost("flexible")]
    public async Task<IActionResult> FlexibleLogQuery([FromBody] QueryLogsFlexible query)
    {
        var result = await mediator.Send(query);
        if (result.IsFailure)
            return BadRequest(result.Errors);
        return Ok(result.Value);
    }

    [HttpDelete("{indexName}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ClearIndexCache(
        [FromRoute] string indexName,
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
