namespace LogService.Application.Options;
public class FallbackProcessingRuntimeOptions
{
    public bool EnableResilient { get; set; } = true;
    public bool EnableDirect { get; set; } = true;
    public bool EnableRetry { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;
}
