namespace LogService.SharedKernel.DTOs;

using System.Text.Json.Serialization;

using LogService.SharedKernel.Enums;

public class LogEntryDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public LogSeverityCode Level { get; set; }
    public string? Message { get; set; }
    public string? Exception { get; set; }
    public string? TraceId { get; set; }
    public string? UserId { get; set; }
    public string? Source { get; set; }
    [JsonPropertyName("@timestamp")]
    public DateTime Timestamp { get; set; }
    public string? Category { get; set; }
    public string? IpAddress { get; set; }
    public string? Code { get; set; }
}
