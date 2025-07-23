namespace SharedKernel.Common.Results.Extensions;

using SharedKernel.Common.Results.Objects;

public static class ResultExtensions
{
    public static Result WithBadRequest(this Result result)
        => result.WithStatusCode(StatusCodes.BadRequest);

    public static Result WithNotFound(this Result result)
        => result.WithStatusCode(StatusCodes.NotFound);

    public static Result WithUnauthorized(this Result result)
        => result.WithStatusCode(StatusCodes.Unauthorized);

    public static Result WithInternalError(this Result result)
        => result.WithStatusCode(StatusCodes.InternalServerError);

    public static Result WithValidationFailure(this Result result)
        => result
            .WithStatusCode(StatusCodes.ValidationFailed)
            .WithErrorType(ErrorType.Validation);

    public static Result WithConflict(this Result result)
        => result.WithStatusCode(StatusCodes.Conflict);

    public static Result WithBusinessRuleViolation(this Result result)
        => result
            .WithStatusCode(StatusCodes.BusinessRuleViolation)
            .WithErrorType(ErrorType.Business);

    // === Generic Versiyonlar ===

    public static Result<T> WithBadRequest<T>(this Result<T> result)
        => result.WithStatusCode(StatusCodes.BadRequest);

    public static Result<T> WithNotFound<T>(this Result<T> result)
        => result.WithStatusCode(StatusCodes.NotFound);

    public static Result<T> WithUnauthorized<T>(this Result<T> result)
        => result.WithStatusCode(StatusCodes.Unauthorized);

    public static Result<T> WithInternalError<T>(this Result<T> result)
        => result.WithStatusCode(StatusCodes.InternalServerError);

    public static Result<T> WithValidationFailure<T>(this Result<T> result)
        => result
            .WithStatusCode(StatusCodes.ValidationFailed)
            .WithErrorType(ErrorType.Validation);

    public static Result<T> WithConflict<T>(this Result<T> result)
        => result.WithStatusCode(StatusCodes.Conflict);

    public static Result<T> WithBusinessRuleViolation<T>(this Result<T> result)
        => result
            .WithStatusCode(StatusCodes.BusinessRuleViolation)
            .WithErrorType(ErrorType.Business);
}
