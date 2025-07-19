namespace LogService.Domain.Policies;

using LogService.SharedKernel.Enums;

public static class RoleLogStageMap
{
    private static readonly IReadOnlyDictionary<UserRole, LogSeverityCode[]> _rules =
        new Dictionary<UserRole, LogSeverityCode[]>
        {
            [UserRole.Admin] = (LogSeverityCode[])Enum.GetValues(typeof(LogSeverityCode)),
            [UserRole.Developer] = (LogSeverityCode[])Enum.GetValues(typeof(LogSeverityCode)),
            [UserRole.Auditor] = new[] { LogSeverityCode.Warning, LogSeverityCode.Error, LogSeverityCode.Fatal },
            [UserRole.User] = new[] { LogSeverityCode.Information, LogSeverityCode.Success }
        };

    public static IReadOnlyCollection<LogSeverityCode> GetAllowedLevels(string role)
    {
        if (Enum.TryParse<UserRole>(role, ignoreCase: true, out var userRole)
            && _rules.TryGetValue(userRole, out var levels))
        {
            return levels;
        }

        return Array.Empty<LogSeverityCode>();
    }
}
