using Shared.Authorization;

namespace ProjectsService.Core.Authorization;

public sealed class ProjectsPermissions : IPermissionMap
{
    public const string ProjectsView = "projects.view";
    public const string ProjectsManage = "projects.manage";

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
            SystemRoles.Admin => [ProjectsView, ProjectsManage],
            SystemRoles.Moderator => [ProjectsView],
            SystemRoles.Employee => [ProjectsView, ProjectsManage],
            SystemRoles.User => [ProjectsView],
            _ => []
        };
}
