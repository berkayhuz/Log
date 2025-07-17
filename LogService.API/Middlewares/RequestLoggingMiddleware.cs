namespace LogService.API.Middlewares;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Options;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Keys;

using Microsoft.Extensions.Options;

public class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogServiceLogger logger,
    IOptions<RequestLoggingOptions> optionsAccessor)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogServiceLogger _logger = logger;
    private readonly RequestLoggingOptions _options = optionsAccessor.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        const string className = nameof(RequestLoggingMiddleware);

        if (IsExcludedPath(context.Request.Path) || IsExcludedContentType(context.Request.ContentType))
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(LogStage.Error, LogMessageDefaults.Messages[LogMessageKeys.Exception_Unhandled_No_Request], ex);
                throw;
            }
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
        else
        {
            requestBody = "(none)";
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
        string? responseBody = null;
        if (_options.LogBody && buffer.Length > 0)
        {
            using var reader = new StreamReader(buffer, leaveOpen: true);
            responseBody = await reader.ReadToEndAsync();
            buffer.Position = 0;

            if (_options.MaskSensitiveData)
            {
                responseBody = MaskSensitiveFields(responseBody);
            }
        }
        else
        {
            responseBody = "(none)";
        }

        await buffer.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        var statusCode = context.Response.StatusCode;
        var elapsed = stopwatch.ElapsedMilliseconds;

        await _logger.LogAsync(
            LogStage.Information,
            $"HTTP {context.Request.Method} {context.Request.Path} responded {statusCode} in {elapsed}ms | Req: {Shrink(requestBody, _options.MaxBodyLength)} | Res: {Shrink(responseBody, _options.MaxBodyLength)}"
        );
    }

    private bool IsExcludedPath(string? path)
        => _options.ExcludedPaths?.Any(p => path?.Contains(p, StringComparison.OrdinalIgnoreCase) == true) ?? false;

    private bool IsExcludedContentType(string? contentType)
        => _options.ExcludedContentTypes?.Any(t => contentType?.StartsWith(t, StringComparison.OrdinalIgnoreCase) == true) ?? false;

    private static string? Shrink(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "(empty)";
        return input!.Length <= maxLength ? input : input[..maxLength] + "...";
    }

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
