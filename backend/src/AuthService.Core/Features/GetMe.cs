using AuthService.Contracts;
using AuthService.Core.Authentication;
using AuthService.Domain.Accounts;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace AuthService.Core.Features;

public sealed record GetMeCommand(ClaimsPrincipal Principal) : ICommand;

public sealed class GetMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/me", HandleAsync);

    [Authorize(AuthenticationSchemes = ApiKeyDefaults.AUTHENTICATION_SCHEME)]
    private static async Task<EndpointResult<GetMeResponse>> HandleAsync(
        HttpContext httpContext,
        [FromServices] GetMeHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new GetMeCommand(httpContext.User), ct);
}

public sealed class GetMeHandler : ICommandHandler<GetMeResponse, GetMeCommand>
{
    private readonly UserManager<Account> _userManager;

    public GetMeHandler(UserManager<Account> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<GetMeResponse, Error>> Handle(
       GetMeCommand command,
       CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(command.Principal);

        if (user is null)
            return Result.Failure<GetMeResponse, Error>(GeneralErrors.Failure("User not found"));

        return Result.Success<GetMeResponse, Error>(new GetMeResponse(user.Id, user.Email));
    }
}