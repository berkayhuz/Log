namespace LogService.Domain.Exceptions;
using System;

public class AuthorizationException : AppException
{
    public AuthorizationException(string message) : base(message) { }
    public AuthorizationException(string message, Exception inner) : base(message, inner) { }
}
