namespace LogService.Application.Options;
using System.Collections.Generic;

public class FetchOptions
{
    public bool FetchCount { get; set; } = true;
    public bool FetchDocuments { get; set; } = true;
    public List<string>? IncludeFields { get; set; }
}

