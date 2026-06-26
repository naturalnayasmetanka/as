using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace AuthService.Core.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Нет заголовка — отдаём NoResult: другие схемы могут попробовать аутентифицировать
        // этот запрос. Fail здесь был бы агрессивен — это не «неверный ключ», а «ключа нет».
        if (!Request.Headers.TryGetValue(ApiKeyDefaults.HEADER_NAME, out var providedKey)
            || string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (string.IsNullOrWhiteSpace(Options.Key))
        {
            Logger.LogError("ApiKey scheme is enabled but Options.Key is not configured.");
            return Task.FromResult(AuthenticateResult.Fail("ApiKey is not configured on the server."));
        }

        if (!string.Equals(providedKey, Options.Key, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "api-key-client"),
            new Claim(ClaimTypes.AuthenticationMethod, ApiKeyDefaults.AUTHENTICATION_SCHEME),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
