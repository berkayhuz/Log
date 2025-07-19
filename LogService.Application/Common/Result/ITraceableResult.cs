namespace LogService.Application.Common.Result;
public interface ITraceableResult
{
    string TraceId { get; set; }
    string IpAddress { get; set; }
}
