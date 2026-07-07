using Core.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using ProjectsService.Core.Authorization;
using Shared.Authorization;

namespace ProjectsService.Core;

public static class Registration
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddHandlers(typeof(Registration).Assembly);
        services.AddValidatorsFromAssembly(typeof(Registration).Assembly);
        services.AddPermissionAuthorization<ProjectsPermissions>(JwtBearerDefaults.AuthenticationScheme);

        return services;
    }
}
