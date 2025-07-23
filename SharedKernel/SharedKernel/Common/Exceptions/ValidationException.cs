namespace SharedKernel.Common.Exceptions;

public class ValidationException : AppException
{
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<(string Field, string Message)> FieldErrors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.ToList();
        FieldErrors = Array.Empty<(string, string)>();
    }

    public ValidationException(IEnumerable<(string Field, string Message)> fieldErrors)
        : base("One or more validation errors occurred.")
    {
        FieldErrors = fieldErrors.ToList();
        Errors = FieldErrors.Select(f => $"{f.Field}: {f.Message}").ToList();
    }

    public ValidationException(string field, string message)
        : this(new[] { (field, message) }) { }
}
