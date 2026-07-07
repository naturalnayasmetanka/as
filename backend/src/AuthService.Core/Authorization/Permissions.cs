namespace AuthService.Core.Authorization;

using Shared.Authorization;

public sealed class Permissions : IPermissionMap
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string ModerationView = "moderation.view";
    public const string StaffDashboardView = "staff.dashboard.view";

    public IReadOnlySet<string> ForRoles(IEnumerable<string> roles)
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
            SystemRoles.ServiceAccount => [UsersView],
            SystemRoles.Moderator => [ModerationView, StaffDashboardView],
            SystemRoles.Employee => [StaffDashboardView],
            _ => []
        };
}
