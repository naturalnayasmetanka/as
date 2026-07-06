using AuthService.Contracts;
using AuthService.Core.Authentication;
using AuthService.Core.Authentication.Abstractions;
using AuthService.Core.Database.Abstractions;
using AuthService.Domain.Accounts;
using AuthService.Domain.RefreshSessions;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace AuthService.Core.Features;

public sealed record JwtLoginCommand(string Email, string Password, HttpContext HttpContext) : ICommand;

public sealed class JwtLoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/jwt/login", HandleAsync);

    private static async Task<EndpointResult<JwtLoginResponse>> HandleAsync(
        [FromBody] JwtLoginRequest request,
        [FromServices] JwtLoginHandler handler,
        HttpContext httpContext,
        CancellationToken ct) =>
        await handler.Handle(new JwtLoginCommand(request.Email, request.Password, httpContext), ct);
}

public sealed class JwtLoginHandler : ICommandHandler<JwtLoginResponse, JwtLoginCommand>
{
    private readonly SignInManager<Account> _signInManager;
    private readonly UserManager<Account> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly RefreshTokenOptions _refreshTokenOptions;

    public JwtLoginHandler(
        SignInManager<Account> signInManager,
        UserManager<Account> userManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IRefreshSessionRepository refreshSessionRepository,
        IOptions<RefreshTokenOptions> refreshTokenOptions)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _refreshSessionRepository = refreshSessionRepository;
        _refreshTokenOptions = refreshTokenOptions.Value;
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

        var accessToken = _jwtTokenService.Create(user);
        var refreshToken = _refreshTokenService.GenerateToken();
        var tokenHash = _refreshTokenService.ComputeTokenHash(refreshToken);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_refreshTokenOptions.ExpireMinutes);

        var session = RefreshSession.Create(user.Id, tokenHash, expiresAt);
        await _refreshSessionRepository.CreateAsync(session, cancellationToken);

        SetRefreshCookie(command.HttpContext, refreshToken, expiresAt);

        return Result.Success<JwtLoginResponse, Error>(
            new JwtLoginResponse(accessToken.AccessToken, accessToken.ExpiresAt));
    }

    private static void SetRefreshCookie(HttpContext httpContext, string refreshToken, DateTimeOffset expiresAt)
    {
        httpContext.Response.Cookies.Append(
            "refresh_token",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !httpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
                SameSite = SameSiteMode.Strict,
                Path = "/auth/jwt",
                Expires = expiresAt
            });

        ClearLegacyRefreshCookie(httpContext);
    }

    private static void ClearLegacyRefreshCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Append(
            "refresh_token",
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !httpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
                SameSite = SameSiteMode.Strict,
                Path = "/auth/jwt/refresh",
                Expires = DateTimeOffset.MinValue
            });
    }
}
