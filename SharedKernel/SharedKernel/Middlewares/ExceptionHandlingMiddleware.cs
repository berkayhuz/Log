namespace LogService.API.Middlewares;

using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Extensions;
using SharedKernel.Common.Results.Objects;

using StatusCodes = SharedKernel.Common.Results.Objects.StatusCodes;

public class ExceptionHandlingMiddleware(RequestDelegate next, IWebHostEnvironment env)
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
            return;

        var result = Result.Failure("An unexpected error occurred.")
                           .WithException(ex)
                           .WithErrorType(ErrorType.Unexpected)
                           .WithStatusCode(StatusCodes.InternalServerError);

        if (env.IsDevelopment())
        {
            result.WithMetadata("StackTrace", ex.ToString());
        }

        var problemDetails = result.ToProblemDetails();

        context.Response.Clear();
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.InternalServerError;
        context.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }
}
