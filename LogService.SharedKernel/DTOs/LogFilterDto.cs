namespace LogService.SharedKernel.DTOs;

public class LogFilterDto
{
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-1);
    public DateTime EndDate { get; set; } = DateTime.UtcNow;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
