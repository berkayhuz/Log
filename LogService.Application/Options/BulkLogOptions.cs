namespace LogService.Application.Options;
using System;

public class BulkLogOptions
{
    public int BatchSize { get; set; } = 1000;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int ChannelCapacity { get; set; } = 10_000;
}
