using AuthService.Domain.Roles;

namespace AuthService.Core.Authorization;

public static class SystemPermissions
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string ModerationView = "moderation.view";
    public const string StaffDashboardView = "staff.dashboard.view";

    public static readonly IReadOnlyCollection<string> All =
    [
        UsersView,
        UsersManage,
        ModerationView,
        StaffDashboardView
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> RolePermissions =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemRoles.User] = [],
            [SystemRoles.Employee] = [StaffDashboardView],
            [SystemRoles.Moderator] = [ModerationView],
            [SystemRoles.Admin] = All
        };

    public static bool IsKnown(string permission) => All.Contains(permission);

    public static IReadOnlyCollection<string> ForRoles(IEnumerable<string> roles)
    {
        return roles
            .SelectMany(role => RolePermissions.TryGetValue(role, out var permissions) ? permissions : [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
