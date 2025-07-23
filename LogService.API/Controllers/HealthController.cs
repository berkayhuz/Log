namespace LogService.API.Controllers;

using LogService.Infrastructure.Services.Elastic.Abstractions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    [HttpGet("elastic-status")]
    public async Task<IActionResult> CheckElasticHealth([FromServices] IElasticHealthService elasticHealthService)
    {
        var result = await elasticHealthService.IsElasticAvailableAsync();

        if (!result.IsSuccess || !result.Value)
        {
            return StatusCode(SharedKernel.Common.Results.Objects.StatusCodes.ServiceUnavailable, new
            {
                elasticStatus = "Down",
                errors = result.Errors,
                exception = result.Exception?.Message,
                statusCode = result.StatusCode
            });
        }

        return Ok(new
        {
            elasticStatus = "Up",
            checkedAt = DateTime.UtcNow.ToString("s")
        });
    }


    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "Live",
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(c => true, cancellationToken);

        var result = new
        {
            status = report.Status.ToString(),
            checkedAt = DateTime.UtcNow.ToString("o"),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds + "ms"
            })
        };

        return StatusCode(report.Status == HealthStatus.Healthy ? 200 : 503, result);
    }

}
