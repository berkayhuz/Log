namespace SharedKernel.Common.Results.Abstractions;
public interface ITraceableResult
{
    string TraceId { get; set; }
    string IpAddress { get; set; }
}
