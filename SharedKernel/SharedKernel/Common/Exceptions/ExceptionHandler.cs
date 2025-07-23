namespace SharedKernel.Common.Exceptions;

using System.Net;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Extensions;
using SharedKernel.Common.Results.Objects;

public static class ExceptionHandler
{
    private static readonly Dictionary<Type, (HttpStatusCode StatusCode, string Message, ErrorType ErrorType)> ExceptionMap = new()
    {
        [typeof(ValidationException)] = (HttpStatusCode.BadRequest, "Validation error.", ErrorType.Validation),
        [typeof(ElasticQueryException)] = (HttpStatusCode.InternalServerError, "Elastic query failed.", ErrorType.Infrastructure),
        [typeof(AuthorizationException)] = (HttpStatusCode.Forbidden, "Authorization denied.", ErrorType.Authorization),
        [typeof(NotFoundException)] = (HttpStatusCode.NotFound, "Resource not found.", ErrorType.NotFound),
        [typeof(AppException)] = (HttpStatusCode.BadRequest, "Application error.", ErrorType.BusinessRule),
    };

    public static Result Handle(Exception ex)
    {
        if (TryMapException(ex, out var result))
            return result;

        return Result.Failure(ex.Message)
            .WithStatusCode((int)HttpStatusCode.InternalServerError)
            .WithException(ex)
            .WithErrorCode(ex.GetType().Name)
            .WithErrorType(ErrorType.Unexpected)
            .WithErrorLevel(ErrorType.Unexpected.ToErrorLevel());
    }

    public static Result<T> Handle<T>(Exception ex)
    {
        if (TryMapException(ex, out var result))
        {
            return Result<T>.Failure(result.Errors)
                .ApplyCommonErrorProperties(result);
        }

        return Result<T>.Failure(ex.Message)
            .WithStatusCode((int)HttpStatusCode.InternalServerError)
            .WithException(ex)
            .WithErrorCode(ex.GetType().Name)
            .WithErrorType(ErrorType.Unexpected)
            .WithErrorLevel(ErrorType.Unexpected.ToErrorLevel());
    }

    private static bool TryMapException(Exception ex, out Result result)
    {
        result = null!;

        var type = ex.GetType();

        if (ExceptionMap.TryGetValue(type, out var mapped))
        {
            result = Result.Failure(GetErrors(ex))
                .WithStatusCode((int)mapped.StatusCode)
                .WithException(ex)
                .WithErrorCode(type.Name)
                .WithErrorType(mapped.ErrorType)
                .WithErrorLevel(mapped.ErrorType.ToErrorLevel());
            return true;
        }

        // Eğer doğrudan eşleşme yoksa, base type'lara bak
        var baseMatch = ExceptionMap.FirstOrDefault(kvp => kvp.Key.IsAssignableFrom(type));
        if (baseMatch.Key is not null)
        {
            var (statusCode, _, errorType) = baseMatch.Value;
            result = Result.Failure(GetErrors(ex))
                .WithStatusCode((int)statusCode)
                .WithException(ex)
                .WithErrorCode(type.Name)
                .WithErrorType(errorType)
                .WithErrorLevel(errorType.ToErrorLevel());
            return true;
        }

        return false;
    }

    private static IEnumerable<string> GetErrors(Exception ex)
    {
        return ex is ValidationException vex && vex.Errors is not null
            ? vex.Errors
            : new[] { ex.Message };
    }

    private static Result<T> ApplyCommonErrorProperties<T>(this Result<T> target, Result source)
    {
        return target
            .WithStatusCode(source.StatusCode ?? 500)
            .WithException(source.Exception!)
            .WithErrorType(source.ErrorType)
            .WithErrorLevel(source.ErrorType.ToErrorLevel()) // Kaynak enum'dan level'ı derive ediyoruz
            .WithCorrelationId(source.CorrelationId ?? string.Empty)
            .WithUser(source.UserId ?? string.Empty)
            .WithTenant(source.TenantId ?? string.Empty);
    }
}
