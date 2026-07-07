using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Shared.Authorization;

public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "permission:";

    private readonly PermissionAuthorizationOptions _permissionOptions;

    public PermissionPolicyProvider(
        IOptions<AuthorizationOptions> options,
        PermissionAuthorizationOptions permissionOptions)
        : base(options)
    {
        _permissionOptions = permissionOptions;
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            return base.GetPolicyAsync(policyName);
        }

        var permission = policyName[PolicyPrefix.Length..];
        var policy = new AuthorizationPolicyBuilder(_permissionOptions.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
