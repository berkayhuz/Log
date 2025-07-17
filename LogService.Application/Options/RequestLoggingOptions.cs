namespace LogService.Application.Options;

public class RequestLoggingOptions
{
    public int MaxBodyLength { get; set; } = 300;
    public string[]? ExcludedPaths { get; set; }
    public string[]? ExcludedContentTypes { get; set; }
    public bool LogBody { get; set; } = true;
    public bool MaskSensitiveData { get; set; } = true;
    public string[] FieldsToMask { get; set; } = new[] { "password", "token", "access_token", "refresh_token", "secret" };
}
