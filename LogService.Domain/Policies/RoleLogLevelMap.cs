namespace LogService.Domain.Policies;

using LogService.SharedKernel.Enums;

public static class RoleLogStageMap
{
    private static readonly IReadOnlyDictionary<UserRole, LogStage[]> _rules =
        new Dictionary<UserRole, LogStage[]>
        {
            [UserRole.Admin] = (LogStage[])Enum.GetValues(typeof(LogStage)),
            [UserRole.Developer] = (LogStage[])Enum.GetValues(typeof(LogStage)),
            [UserRole.Auditor] = new[] { LogStage.Warning, LogStage.Error, LogStage.Fatal },
            [UserRole.User] = new[] { LogStage.Information, LogStage.Success }
        };

    public static IReadOnlyCollection<LogStage> GetAllowedLevels(string role)
    {
        if (Enum.TryParse<UserRole>(role, ignoreCase: true, out var userRole)
            && _rules.TryGetValue(userRole, out var levels))
        {
            return levels;
        }

        return Array.Empty<LogStage>();
    }
}
