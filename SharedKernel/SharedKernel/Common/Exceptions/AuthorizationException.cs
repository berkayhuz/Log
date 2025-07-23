namespace SharedKernel.Common.Exceptions;
using System;

public class AuthorizationException : AppException
{
    public AuthorizationException(string message) : base(message) { }
    public AuthorizationException(string message, Exception? innerException) : base(message, innerException) { }
}
