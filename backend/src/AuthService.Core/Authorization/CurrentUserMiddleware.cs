using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AuthService.Core.Authorization;

public sealed class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ICurrentUser currentUser)
    {
        var bearerResult = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        currentUser.Set(bearerResult.Succeeded && bearerResult.Principal is not null
            ? bearerResult.Principal
            : httpContext.User);

        await _next(httpContext);
    }
}

public static class CurrentUserMiddlewareExtensions
{
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<CurrentUserMiddleware>();
}
