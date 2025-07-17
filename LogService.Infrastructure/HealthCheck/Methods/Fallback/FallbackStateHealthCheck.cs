namespace LogService.Infrastructure.HealthCheck.Methods.Fallback;
using System.Collections.Generic;
using System.Threading.Tasks;

using LogService.Application.Abstractions.Fallback;
using LogService.Infrastructure.HealthCheck.Metadata;

using Microsoft.Extensions.Diagnostics.HealthChecks;

[Name("fallback_state_check")]
[HealthTags("fallback", "state", "config", "ready")]
public class FallbackStateHealthCheck(IFallbackProcessingStateService stateService) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var state = stateService.Current;

        if (!state.EnableResilient && !state.EnableDirect && !state.EnableRetry)
        {
            return Task.FromResult(HealthCheckResult.Degraded("All fallback modes are disabled."));
        }

        var activeModes = new List<string>();
        if (state.EnableResilient) activeModes.Add("Resilient");
        if (state.EnableDirect) activeModes.Add("Direct");
        if (state.EnableRetry) activeModes.Add("Retry");

        var description = $"Active Fallback Modes: {string.Join(", ", activeModes)} | Interval: {state.IntervalSeconds}s";

        return Task.FromResult(HealthCheckResult.Healthy(description));
    }
}
