using AuthService.Contracts;
using AuthService.Core.Authentication;
using AuthService.Domain.Accounts;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AuthService.Core.Features;

public sealed record JwtLoginCommand(string Email, string Password) : ICommand;

public sealed class JwtLoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/jwt/login", HandleAsync);

    private static async Task<EndpointResult<JwtLoginResponse>> HandleAsync(
        [FromBody] JwtLoginRequest request,
        [FromServices] JwtLoginHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new JwtLoginCommand(request.Email, request.Password), ct);
}

public sealed class JwtLoginHandler : ICommandHandler<JwtLoginResponse, JwtLoginCommand>
{
    private readonly SignInManager<Account> _signInManager;
    private readonly UserManager<Account> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public JwtLoginHandler(
        SignInManager<Account> signInManager,
        UserManager<Account> userManager,
        IJwtTokenService jwtTokenService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<JwtLoginResponse, Error>> Handle(
        JwtLoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);

        if (user is null)
            return Result.Failure<JwtLoginResponse, Error>(
                GeneralErrors.Failure("Invalid credentials"));

        var check = await _signInManager.CheckPasswordSignInAsync(
            user,
            command.Password,
            lockoutOnFailure: true);

        if (!check.Succeeded)
            return Result.Failure<JwtLoginResponse, Error>(
                GeneralErrors.Failure("Invalid credentials"));

        var token = _jwtTokenService.Create(user);

        return Result.Success<JwtLoginResponse, Error>(
            new JwtLoginResponse(token.AccessToken, token.ExpiresAt));
    }
}