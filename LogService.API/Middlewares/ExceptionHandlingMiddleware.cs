namespace LogService.API.Middlewares;

using System.Text.Json;

using LogService.Application.Common.Exception;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IWebHostEnvironment env)
{
    public async Task Invoke(HttpContext context)
    {
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
        if (context.Response.HasStarted || !context.Response.Body.CanWrite)
        {
            return;
        }

        var details = ExceptionHandler.Handle(ex);

        context.Response.Clear();
        context.Response.StatusCode = (int)details.StatusCode;
        context.Response.ContentType = "application/json";

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
