namespace LogService.Domain.DTOs;
using System.Collections.Generic;

public class SearchResult<T>
{
    public bool IsValid { get; init; }
    public List<T>? Documents { get; init; }
    public long? TotalCount { get; init; }
    public string? ErrorReason { get; init; }
}

