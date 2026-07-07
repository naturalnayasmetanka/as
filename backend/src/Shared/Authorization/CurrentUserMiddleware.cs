using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Shared.Authorization;

public sealed class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PermissionAuthorizationOptions _options;

    public CurrentUserMiddleware(RequestDelegate next, PermissionAuthorizationOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, CurrentUser currentUser)
    {
        var bearerResult = await context.AuthenticateAsync(_options.AuthenticationScheme);
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
