namespace LogService.API.Controllers;

using LogService.Application.Abstractions.Elastic;

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
        var isUp = await elasticHealthService.IsElasticAvailableAsync();
        return Ok(new { ElasticStatus = isUp ? "Up" : "Down" });
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
            timestamp = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm")
        });
    }


    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(c => true, cancellationToken);

        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };

        return StatusCode(report.Status == HealthStatus.Healthy ? 200 : 503, result);
    }
}
