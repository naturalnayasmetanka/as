using AuthService.Core.Authentication;
using AuthService.Core.Authentication.Abstractions;
using Core.Abstractions;
using FluentValidation;
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
        services
            .AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .ValidateOnStart();

        return services;
    }
}
