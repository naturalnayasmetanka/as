using System.Security.Claims;

namespace Shared.Authorization;

public sealed class CurrentUser
{
    private readonly IPermissionMap _permissionMap;

    public CurrentUser(IPermissionMap permissionMap)
    {
        _permissionMap = permissionMap;
    }

    public Guid? Id { get; private set; }
    public IReadOnlySet<string> Roles { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> Permissions { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public void Set(ClaimsPrincipal principal)
    {
        if (Guid.TryParse(principal.FindFirstValue("sub"), out var id))
        {
            Id = id;
        }

        var roles = principal
            .FindAll("role")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();

        Roles = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
        Permissions = _permissionMap.ForRoles(Roles);
    }
}
