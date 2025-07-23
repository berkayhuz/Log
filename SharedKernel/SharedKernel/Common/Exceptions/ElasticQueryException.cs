namespace SharedKernel.Common.Exceptions;

public class ElasticQueryException : AppException
{
    public ElasticQueryException(string message) : base(message) { }
    public ElasticQueryException(string message, Exception? innerException) : base(message, innerException) { }
}
