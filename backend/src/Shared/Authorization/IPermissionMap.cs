namespace Shared.Authorization;

public interface IPermissionMap
{
    IReadOnlySet<string> ForRoles(IEnumerable<string> roles);
}
