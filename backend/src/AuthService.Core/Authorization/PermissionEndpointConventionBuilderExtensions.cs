using Microsoft.AspNetCore.Builder;

namespace AuthService.Core.Authorization;

public static class PermissionEndpointConventionBuilderExtensions
{
    public static TBuilder RequirePermissions<TBuilder>(
        this TBuilder builder,
        params string[] permissions)
        where TBuilder : IEndpointConventionBuilder
    {
        foreach (var permission in permissions)
        {
            builder.RequireAuthorization($"{PermissionPolicyProvider.PolicyPrefix}{permission}");
        }

        return builder;
    }
}
