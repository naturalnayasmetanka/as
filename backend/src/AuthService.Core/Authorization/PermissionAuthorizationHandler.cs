using Microsoft.AspNetCore.Authorization;

namespace AuthService.Core.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly CurrentUser _currentUser;

    public PermissionAuthorizationHandler(CurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (_currentUser.Permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
