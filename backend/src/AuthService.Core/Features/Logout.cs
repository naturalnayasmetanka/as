using AuthService.Contracts;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Mvc;
using AuthService.Domain.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace AuthService.Core.Features;

public sealed class LogoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/logout", HandleAsync);

    [Authorize]
    private static async Task<EndpointResult<object>> HandleAsync(
        [FromServices] SignInManager<Account> signInManager,
        CancellationToken ct)
    {
        await signInManager.SignOutAsync();

        return Result.Success<object, Error>(null!);
    }
}
