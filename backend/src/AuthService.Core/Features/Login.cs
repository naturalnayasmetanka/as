using AuthService.Contracts;
using AuthService.Domain.Accounts;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace AuthService.Core.Features;

public sealed record LoginCommand(string Email, string Password) : ICommand;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/login", HandleAsync);

    private static async Task<EndpointResult<LoginResponse>> HandleAsync(
        [FromBody] LoginRequest request,
        [FromServices] LoginHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new LoginCommand(request.Email, request.Password), ct);
}

public sealed class LoginHandler : ICommandHandler<LoginResponse, LoginCommand>
{
    private readonly SignInManager<Account> _signInManager;
    private readonly UserManager<Account> _userManager;

    public LoginHandler(SignInManager<Account> signInManager, UserManager<Account> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<Result<LoginResponse, Error>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
            return Result.Failure<LoginResponse, Error>(GeneralErrors.Failure("Invalid credentials"));

        var user = await _userManager.FindByEmailAsync(command.Email);

        if (user is null)
            return Result.Failure<LoginResponse, Error>(GeneralErrors.Failure("Invalid credentials"));

        var email = user.Email;
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<LoginResponse, Error>(GeneralErrors.Failure("Invalid credentials"));

        var check = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);

        if (!check.Succeeded)
            return Result.Failure<LoginResponse, Error>(GeneralErrors.Failure("Invalid credentials"));

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", user.Id.ToString()),
            new Claim("security_stamp", user.SecurityStamp ?? string.Empty),
            new Claim(ClaimTypes.Name, email)
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        await _signInManager.SignInWithClaimsAsync(user, isPersistent: true, claims);

        return Result.Success<LoginResponse, Error>(new LoginResponse(user.Id, email));
    }
}
