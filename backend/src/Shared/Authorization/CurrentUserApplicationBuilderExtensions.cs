using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Authorization;

public static class CurrentUserApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<PermissionAuthorizationOptions>();
        return app.UseMiddleware<CurrentUserMiddleware>(options);
    }
}
