namespace SharedKernel.Common.Results.Extensions;

using SharedKernel.Common.Results.Objects;

public static class ErrorTypeExtensions
{
    public static ErrorLevel ToErrorLevel(this ErrorType type) => type switch
    {
        ErrorType.None => ErrorLevel.Information,
        ErrorType.Validation => ErrorLevel.Warning,
        ErrorType.Domain => ErrorLevel.Warning,
        ErrorType.NotFound => ErrorLevel.Warning,
        ErrorType.Unauthorized => ErrorLevel.Warning,
        ErrorType.Forbidden => ErrorLevel.Warning,
        ErrorType.Conflict => ErrorLevel.Warning,
        ErrorType.Timeout => ErrorLevel.Error,
        ErrorType.Canceled => ErrorLevel.Information,
        ErrorType.Infrastructure => ErrorLevel.Error,
        ErrorType.Unexpected => ErrorLevel.Fatal,
        ErrorType.ServiceUnavailable => ErrorLevel.Critical,
        ErrorType.Security => ErrorLevel.Fatal,
        _ => ErrorLevel.Error
    };
}
