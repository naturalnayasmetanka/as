using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Authentication;
using Shared.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectsService.Core.AuthService;

public sealed class CachedJwtServiceTokenProvider : IServiceTokenProvider
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RefreshBeforeExpiration = TimeSpan.FromSeconds(30);

    private readonly JwtOptions _jwtOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public CachedJwtServiceTokenProvider(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (IsCurrentTokenUsable())
        {
            return _token!;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (IsCurrentTokenUsable())
            {
                return _token!;
            }

            (_token, _expiresAt) = CreateToken();
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsCurrentTokenUsable() =>
        _token is not null && DateTimeOffset.UtcNow < _expiresAt.Subtract(RefreshBeforeExpiration);

    private (string Token, DateTimeOffset ExpiresAt) CreateToken()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(TokenLifetime);
        var serviceId = Guid.CreateVersion7().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, serviceId),
            new Claim(JwtRegisteredClaimNames.Name, "ProjectsService"),
            new Claim("client_id", "projects-service"),
            new Claim("role", SystemRoles.ServiceAccount),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
