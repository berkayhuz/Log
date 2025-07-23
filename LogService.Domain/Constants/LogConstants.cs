namespace LogService.Domain.Constants;
public static class LogConstants
{
    public const string DataStreamName = "logservice-logs";
    public static string DefaultIndexWildcard => $"{DataStreamName}-*";
    public static string DefaultRegionKey => $"region:{DataStreamName}";
}
