using AuthService.Core.Authentication.Abstractions;
using AuthService.Core.Database.Abstractions;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.Core.Features;

public sealed record JwtLogoutCommand(HttpContext HttpContext) : ICommand;

public sealed record EmptyResponse;

public sealed class JwtLogoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/jwt/logout", HandleAsync)
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            });

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    private static async Task<EndpointResult<EmptyResponse>> HandleAsync(
        HttpContext httpContext, [FromServices] JwtLogoutHandler handler, CancellationToken ct) 
        => await handler.Handle(new JwtLogoutCommand(httpContext), ct);
}

public sealed class JwtLogoutHandler : ICommandHandler<EmptyResponse, JwtLogoutCommand>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshSessionRepository _refreshSessionRepository;

    public JwtLogoutHandler(
        IRefreshTokenService refreshTokenService,
        IRefreshSessionRepository refreshSessionRepository)
    {
        _refreshTokenService = refreshTokenService;
        _refreshSessionRepository = refreshSessionRepository;
    }

    public async Task<Result<EmptyResponse, Error>> Handle(
        JwtLogoutCommand command,
        CancellationToken cancellationToken)
    {
        var userIdClaim = command.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim?.Value is not { } userIdString || !Guid.TryParse(userIdString, out _))
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure("User ID not found in token"));

        // Находим refresh-сессию
        if (command.HttpContext.Request.Cookies.TryGetValue("refresh_token", out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            var tokenHash = _refreshTokenService.ComputeTokenHash(refreshToken);
            var session = await _refreshSessionRepository.FindValidByTokenHashAsync(tokenHash, cancellationToken);

            if (session is not null)
                await _refreshSessionRepository.RevokeAsync(session.Id, cancellationToken);
        }

        // Очищаем cookie
        ClearRefreshCookie(command.HttpContext);

        return Result.Success<EmptyResponse, Error>(new EmptyResponse());
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
