namespace LogService.Application.Common.Exception;
using System;
using System.Net;

using LogService.Domain.Exceptions;
using LogService.SharedKernel.Keys;

public static class ExceptionHandler
{
    private static readonly Dictionary<Type, (HttpStatusCode, string)> ExceptionMap = new()
    {
        [typeof(ValidationException)] = (HttpStatusCode.BadRequest, LogMessageDefaults.Messages[LogMessageKeys.Exception_ValidationError]),
        [typeof(ElasticQueryException)] = (HttpStatusCode.InternalServerError,LogMessageDefaults.Messages[LogMessageKeys.Exception_ElasticQueryFailed]),
        [typeof(AppException)] = (HttpStatusCode.BadRequest,LogMessageDefaults.Messages[LogMessageKeys.Exception_AppError]),
        [typeof(AuthorizationException)] = (HttpStatusCode.Forbidden,LogMessageDefaults.Messages[LogMessageKeys.Exception_AuthorizationError]),
        [typeof(NotFoundException)] = (HttpStatusCode.NotFound,LogMessageDefaults.Messages[LogMessageKeys.Exception_NotFound]),
    };

    public static ExceptionDetails Handle(Exception ex)
    {
        if (ExceptionMap.TryGetValue(ex.GetType(), out var value))
        {
            IEnumerable<string> errors = ex is ValidationException vex ? vex.Errors : new[] { ex.Message };
            return new ExceptionDetails(value.Item1, value.Item2, errors);
        }
        return new ExceptionDetails(
            HttpStatusCode.InternalServerError,
            LogMessageDefaults.Messages[LogMessageKeys.Exception_Unexpected],
            new[] { ex.Message }
        );
    }
}
