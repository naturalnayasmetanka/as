using Microsoft.AspNetCore.Builder;

namespace AuthService.Core.Authorization;

public static class RequirePermissionsExtensions
{
    public static TBuilder RequirePermissions<TBuilder>(
        this TBuilder builder,
        string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(permission);
    }
}
