namespace SharedKernel.Common.Exceptions;
using System;

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string message, Exception? innerException) : base(message, innerException) { }
}
