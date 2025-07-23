namespace LogService.Domain.Policies;

using SharedKernel.Common.Results.Objects;
using SharedKernel.Enums;

public static class RoleLogStageMap
{
    private static readonly IReadOnlyDictionary<UserRole, ErrorLevel[]> _rules = new Dictionary<UserRole, ErrorLevel[]>
    {
        [UserRole.Admin] = Enum.GetValues<ErrorLevel>(),
        [UserRole.Developer] = Enum.GetValues<ErrorLevel>(),
        [UserRole.Auditor] = new[] { ErrorLevel.Warning, ErrorLevel.Error, ErrorLevel.Fatal },
        [UserRole.User] = new[] { ErrorLevel.Information, ErrorLevel.Success }
    };

    public static IReadOnlyCollection<ErrorLevel> GetAllowedLevels(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Array.Empty<ErrorLevel>();

        if (Enum.TryParse(role, ignoreCase: true, out UserRole userRole) &&
            _rules.TryGetValue(userRole, out var levels))
        {
            return levels;
        }

        return Array.Empty<ErrorLevel>();
    }
}
