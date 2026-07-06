using System.Security.Claims;

namespace AuthService.Core.Authorization;

public interface ICurrentUser
{
    Guid? Id { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool HasPermission(string permission);
    void Set(ClaimsPrincipal principal);
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly List<string> _roles = [];
    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    public Guid? Id { get; private set; }
    public IReadOnlyCollection<string> Roles => _roles;
    public IReadOnlyCollection<string> Permissions => _permissions;

    public bool HasPermission(string permission) => _permissions.Contains(permission);

    public void Set(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        _roles.Clear();
        _permissions.Clear();
        Id = null;

        if (principal.Identity?.IsAuthenticated != true)
            return;

        var idClaim = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idClaim?.Value, out var userId))
            Id = userId;

        _roles.AddRange(
            principal.FindAll("role")
                .Concat(principal.FindAll(ClaimTypes.Role))
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase));

        foreach (var permission in SystemPermissions.ForRoles(_roles))
        {
            _permissions.Add(permission);
        }
    }
}
