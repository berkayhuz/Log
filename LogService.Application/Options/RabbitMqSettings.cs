namespace LogService.Application.Options;
public class RabbitMqSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string LogQueueName { get; set; } = "log_queue";
    public ushort PrefetchCount { get; set; } = 10;
}
