using AuthService.Core.Authentication;
using AuthService.Core.Authentication.Abstractions;
using AuthService.Core.Authorization;
using Core.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Core;

public static class Registration
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHandlers(typeof(Registration).Assembly);
        services.AddValidatorsFromAssembly(typeof(Registration).Assembly);

        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<CurrentUser>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services
            .AddOptions<BootstrapAdminOptions>()
            .Bind(configuration.GetSection(BootstrapAdminOptions.SectionName));

        services
            .AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .ValidateOnStart();

        return services;
    }
}
