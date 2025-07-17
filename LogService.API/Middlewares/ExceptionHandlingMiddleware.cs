namespace LogService.API.Middlewares;

using System.Net;
using System.Text.Json;

using LogService.Application.Common.Exception;
using LogService.Application.Options;

using Microsoft.Extensions.Options;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IWebHostEnvironment env,
    IOptions<ExceptionHandlingMiddlewareOptions> options)
{
    private readonly string _jsonContentType = "application/json";

    public async Task Invoke(HttpContext context)
    {
        const string className = nameof(ExceptionHandlingMiddleware);
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        const string className = nameof(ExceptionHandlingMiddleware);

        if (context.Response.HasStarted || !context.Response.Body.CanWrite)
        {
            return;
        }

        var details = ExceptionHandler.Handle(ex);

        context.Response.Clear();
        context.Response.StatusCode = (int)details.StatusCode;
        context.Response.ContentType = _jsonContentType;

        var response = new
        {
            status = context.Response.StatusCode,
            message = details.Message,
            errors = details.Errors,
            traceId = context.TraceIdentifier,
            stackTrace = env.IsDevelopment() ? ex.ToString() : null
        };

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}
