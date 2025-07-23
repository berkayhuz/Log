namespace LogService.API.Middlewares;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Extensions;
using SharedKernel.Common.Results.Objects;
using SharedKernel.Options;

public class RequestLoggingMiddleware(
    RequestDelegate next,
    IOptions<RequestLoggingOptions> optionsAccessor,
    ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly RequestLoggingOptions _options = optionsAccessor.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExcludedPath(context.Request.Path) || IsExcludedContentType(context.Request.ContentType))
        {
            await _next(context);
            return;
        }

        string? requestBody = null;
        if (_options.LogBody && context.Request.ContentLength > 0 && context.Request.Body.CanRead)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (_options.MaskSensitiveData)
            {
                requestBody = MaskSensitiveFields(requestBody);
            }
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
        }

        buffer.Position = 0;
        string responseBody = "(none)";
        Result? result = null;

        if (_options.LogBody && buffer.Length > 0)
        {
            using var reader = new StreamReader(buffer, leaveOpen: true);
            responseBody = await reader.ReadToEndAsync();
            buffer.Position = 0;

            if (_options.MaskSensitiveData)
                responseBody = MaskSensitiveFields(responseBody);

            try
            {
                result = JsonSerializer.Deserialize<Result>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                // Loglama dışında kalan durumlar (örneğin düz string response)
            }
        }

        await buffer.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        var statusCode = context.Response.StatusCode;
        var elapsed = stopwatch.ElapsedMilliseconds;

        if (result != null && result.IsFailure)
        {
            var level = result.ErrorType.ToErrorLevel(); // extension
            var logMessage = new StringBuilder();
            logMessage.AppendLine("HTTP Request Failed:");
            logMessage.AppendLine($"- Method: {context.Request.Method}");
            logMessage.AppendLine($"- Path: {context.Request.Path}");
            logMessage.AppendLine($"- Status: {statusCode}");
            logMessage.AppendLine($"- Elapsed: {elapsed}ms");
            logMessage.AppendLine($"- Errors: {string.Join(", ", result.Errors)}");
            logMessage.AppendLine($"- ErrorType: {result.ErrorType}");
            logMessage.AppendLine($"- ErrorLevel: {result.ErrorLevel}");
            logMessage.AppendLine($"- CorrelationId: {result.CorrelationId}");
            logMessage.AppendLine($"- Metadata: {JsonSerializer.Serialize(result.Metadata)}");

            logger.Log(level switch
            {
                ErrorLevel.Critical or ErrorLevel.Fatal => LogLevel.Critical,
                ErrorLevel.Error => LogLevel.Error,
                ErrorLevel.Warning => LogLevel.Warning,
                _ => LogLevel.Information
            }, logMessage.ToString());
        }
        else
        {
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms\nRequest: {RequestBody}\nResponse: {ResponseBody}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                elapsed,
                requestBody ?? "(none)",
                responseBody
            );
        }
    }

    private bool IsExcludedPath(string? path) =>
        _options.ExcludedPaths?.Any(p => path?.Contains(p, StringComparison.OrdinalIgnoreCase) == true) ?? false;

    private bool IsExcludedContentType(string? contentType) =>
        _options.ExcludedContentTypes?.Any(t => contentType?.StartsWith(t, StringComparison.OrdinalIgnoreCase) == true) ?? false;

    private string MaskSensitiveFields(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input!;

        string[] fieldsToMask = _options.FieldsToMask ?? Array.Empty<string>();
        string result = input;

        foreach (var field in fieldsToMask)
        {
            var pattern = $"(\"{field}\"\\s*:\\s*\")([^\"]*)(\")";
            result = Regex.Replace(
                result,
                pattern,
                "$1***MASKED***$3",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
        }

        return result;
    }
}
