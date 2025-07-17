namespace LogService.Application.Common.Exception;
using System.Collections.Generic;
using System.Net;

public record ExceptionDetails(HttpStatusCode StatusCode, string Message, IEnumerable<string>? Errors = null);
