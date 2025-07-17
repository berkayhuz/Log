namespace LogService.Domain.Exceptions;

public class ElasticQueryException : AppException
{
    public ElasticQueryException(string message)
        : base(message) { }

    public ElasticQueryException(string message, Exception inner)
        : base(message, inner) { }
}
