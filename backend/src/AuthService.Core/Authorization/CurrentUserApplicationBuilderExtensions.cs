using Microsoft.AspNetCore.Builder;

namespace AuthService.Core.Authorization;

public static class CurrentUserApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentUserMiddleware>();
}
