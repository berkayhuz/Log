namespace LogService.Domain.DTOs;
public class LogFilterDto
{
    private const int MaxPageSize = 500;
    private int _pageSize = 50;
    private int _page = 1;

    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-1);
    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    public int Page
    {
        get => _page < 1 ? 1 : _page;
        set => _page = value;
    }

    public int PageSize
    {
        get => _pageSize > MaxPageSize ? MaxPageSize : _pageSize;
        set => _pageSize = value;
    }
}

