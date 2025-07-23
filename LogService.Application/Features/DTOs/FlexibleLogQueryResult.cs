namespace LogService.Application.Features.DTOs;
using System.Collections.Generic;

using LogService.Domain.DTOs;

public class FlexibleLogQueryResult
{
    public long? TotalCount { get; set; }
    public List<LogEntryDto>? Documents { get; set; }
}
