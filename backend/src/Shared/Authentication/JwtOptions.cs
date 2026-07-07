namespace Shared.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpireMinutes { get; init; } = 15;
    public string SigningKey { get; init; } = string.Empty;
}
