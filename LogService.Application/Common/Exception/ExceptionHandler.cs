namespace LogService.Application.Common.Exception;
using System;
using System.Net;

using LogService.Domain.Exceptions;

public static class ExceptionHandler
{
    private static readonly Dictionary<Type, (HttpStatusCode StatusCode, string Message)> ExceptionMap = new()
    {
        [typeof(ValidationException)] = (HttpStatusCode.BadRequest, "Validation error."),
        [typeof(ElasticQueryException)] = (HttpStatusCode.InternalServerError, "Elastic query failed."),
        [typeof(AppException)] = (HttpStatusCode.BadRequest, "Application error."),
        [typeof(AuthorizationException)] = (HttpStatusCode.Forbidden, "Authorization denied."),
        [typeof(NotFoundException)] = (HttpStatusCode.NotFound, "Resource not found."),
    };

    public static ExceptionDetails Handle(Exception ex)
    {
        if (ExceptionMap.TryGetValue(ex.GetType(), out var value))
        {
            IEnumerable<string> errors = ex is ValidationException vex ? vex.Errors : new[] { ex.Message };

            return new ExceptionDetails(
                value.StatusCode,
                value.Message,
                errors,
                code: null
            );
        }

        return new ExceptionDetails(
            HttpStatusCode.InternalServerError,
            "Unexpected server error.",
            new[] { ex.Message },
            code: null
        );
    }
}
