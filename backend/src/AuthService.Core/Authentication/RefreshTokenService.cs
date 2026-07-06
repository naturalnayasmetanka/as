using AuthService.Core.Authentication.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Core.Authentication;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly RefreshTokenOptions _options;

    public RefreshTokenService(IOptions<RefreshTokenOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken()
    {
        var buffer = new byte[_options.TokenLengthBytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(buffer);
        return Convert.ToBase64String(buffer);
    }

    public string ComputeTokenHash(string token)
    {
        var input = _options.Pepper is not null
            ? $"{token}{_options.Pepper}"
            : token;

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
