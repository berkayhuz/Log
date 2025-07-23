namespace SharedKernel.Common.Results.Objects;
public enum ErrorCode
{
    None = 0,

    // Genel
    UnknownError,
    UnexpectedError,
    OperationCanceled,

    // Kullanıcı
    UserNotFound,
    UserAlreadyExists,
    InvalidCredentials,
    EmailAlreadyInUse,
    EmailNotVerified,

    // Auth
    TokenExpired,
    TokenInvalid,
    RefreshTokenNotFound,
    AccessDenied,

    // Doğrulama
    InvalidEmail,
    PasswordTooWeak,
    FieldIsRequired,
    FieldTooLong,
    FieldTooShort,

    // Domain
    BusinessRuleViolation,
    DuplicateEntity,
    EntityInUse,

    // Altyapı
    DatabaseUnavailable,
    DatabaseWriteFailed,
    CacheUnavailable,
    FileUploadFailed,
    SerializationFailure,

    // Güvenlik
    SecurityBreach,
    TooManyRequests,
    SuspiciousActivity,

    // Harici
    ExternalServiceUnavailable,
    ExternalServiceError,
    IntegrationFailed
}
