namespace LogService.SharedKernel.Requests;
public class UpdateLogMessageRequest
{
    public required string Key { get; set; }
    public required string Message { get; set; }
}
