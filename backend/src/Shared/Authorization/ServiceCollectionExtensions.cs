using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Authorization;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPermissionAuthorization<TPermissionMap>(
        this IServiceCollection services,
        string authenticationScheme)
        where TPermissionMap : class, IPermissionMap
    {
        services.AddSingleton(new PermissionAuthorizationOptions
        {
            AuthenticationScheme = authenticationScheme
        });
        services.AddScoped<CurrentUser>();
        services.AddScoped<IPermissionMap, TPermissionMap>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        return services;
    }
}
