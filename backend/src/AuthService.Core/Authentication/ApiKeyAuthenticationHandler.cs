using AuthService.Domain.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<Account> _userManager;
    public ApiKeyAuthenticationHandler(
        UserManager<Account> userManager,
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _userManager = userManager;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;         
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer "))
            return AuthenticateResult.NoResult();                 

        string token = header["Bearer ".Length..].Trim();
        Account? user = await _userManager.FindByIdAsync(token);
        if (user is null)
            return AuthenticateResult.Fail("Unknown token");

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
    };
        var identity = new ClaimsIdentity(claims, Scheme.Name);   
        var principal = new ClaimsPrincipal(identity);          
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
