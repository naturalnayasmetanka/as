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

public sealed record JwtRefreshCommand(string RefreshToken, HttpContext HttpContext) : ICommand;

public sealed record JwtRefreshResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed class JwtRefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/jwt/refresh", HandleAsync);

    private static async Task<EndpointResult<JwtRefreshResponse>> HandleAsync(
        HttpContext httpContext,
        [FromServices] JwtRefreshHandler handler,
        CancellationToken ct)
    {
        if (!httpContext.Request.Cookies.TryGetValue("refresh_token", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            refreshToken = string.Empty;
        }

        return await handler.Handle(new JwtRefreshCommand(refreshToken, httpContext), ct);
    }
}

public sealed class JwtRefreshHandler : ICommandHandler<JwtRefreshResponse, JwtRefreshCommand>
{
    private readonly UserManager<Account> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly RefreshTokenOptions _refreshTokenOptions;

    public JwtRefreshHandler(
        UserManager<Account> userManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IRefreshSessionRepository refreshSessionRepository,
        IOptions<RefreshTokenOptions> refreshTokenOptions)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _refreshSessionRepository = refreshSessionRepository;
        _refreshTokenOptions = refreshTokenOptions.Value;
    }

    public async Task<Result<JwtRefreshResponse, Error>> Handle(
        JwtRefreshCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            ClearRefreshCookie(command.HttpContext);
            return Result.Failure<JwtRefreshResponse, Error>(
                GeneralErrors.Failure("Refresh token not found"));
        }

        var tokenHash = _refreshTokenService.ComputeTokenHash(command.RefreshToken);
        var session = await _refreshSessionRepository.FindValidByTokenHashAsync(tokenHash, cancellationToken);

        if (session is null)
        {
            ClearRefreshCookie(command.HttpContext);
            return Result.Failure<JwtRefreshResponse, Error>(
                GeneralErrors.Failure("Invalid or expired refresh token"));
        }

        var user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            ClearRefreshCookie(command.HttpContext);
            return Result.Failure<JwtRefreshResponse, Error>(
                GeneralErrors.Failure("User not found"));
        }

        // Ротация: гасим старый refresh и создаём новый
        var newRefreshToken = _refreshTokenService.GenerateToken();
        var newTokenHash = _refreshTokenService.ComputeTokenHash(newRefreshToken);
        var newExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_refreshTokenOptions.ExpireMinutes);

        // Отзываем старую сессию
        session.Revoke();
        await _refreshSessionRepository.RevokeAsync(session.Id, cancellationToken);

        // Создаём новую сессию как потомка старой (для отслеживания цепочки)
        var newSession = RefreshSession.Create(
            session.UserId,
            newTokenHash,
            newExpiresAt,
            parentSessionId: session.Id);
        await _refreshSessionRepository.CreateAsync(newSession, cancellationToken);

        // Создаём новый access-токен
        var accessToken = _jwtTokenService.Create(user);

        // Устанавливаем новый refresh cookie
        SetRefreshCookie(command.HttpContext, newRefreshToken, newExpiresAt);

        return Result.Success<JwtRefreshResponse, Error>(
            new JwtRefreshResponse(accessToken.AccessToken, accessToken.ExpiresAt));
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

    private static void ClearRefreshCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Append(
            "refresh_token",
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !httpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
                SameSite = SameSiteMode.Strict,
                Path = "/auth/jwt",
                Expires = DateTimeOffset.MinValue
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
