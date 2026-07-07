using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AuthService.Core.Authorization;

public sealed class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentUser currentUser)
    {
        var bearerResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (bearerResult.Succeeded && bearerResult.Principal is not null)
        {
            context.User = bearerResult.Principal;
            currentUser.Set(bearerResult.Principal);
        }
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            currentUser.Set(context.User);
        }

        await _next(context);
    }
}
