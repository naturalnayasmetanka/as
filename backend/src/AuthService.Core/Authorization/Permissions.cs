namespace AuthService.Core.Authorization;

public static class Permissions
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string ModerationView = "moderation.view";
    public const string StaffDashboardView = "staff.dashboard.view";

    public static IReadOnlySet<string> ForRoles(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            foreach (var permission in ForRole(role))
            {
                permissions.Add(permission);
            }
        }

        return permissions;
    }

    private static IReadOnlyCollection<string> ForRole(string role) =>
        role switch
        {
            SystemRoles.Admin => [UsersView, UsersManage, ModerationView, StaffDashboardView],
            SystemRoles.Moderator => [ModerationView, StaffDashboardView],
            SystemRoles.Employee => [StaffDashboardView],
            _ => []
        };
}
