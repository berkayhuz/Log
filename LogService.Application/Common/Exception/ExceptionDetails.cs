namespace LogService.Application.Common.Exception;
using System.Collections.Generic;
using System.Net;

public class ExceptionDetails
{
    public HttpStatusCode StatusCode { get; }
    public string Message { get; }
    public IEnumerable<string> Errors { get; }
    public string? Code { get; }

    public ExceptionDetails(HttpStatusCode statusCode, string message, IEnumerable<string> errors, string? code = null)
    {
        StatusCode = statusCode;
        Message = message;
        Errors = errors;
        Code = code;
    }
}

