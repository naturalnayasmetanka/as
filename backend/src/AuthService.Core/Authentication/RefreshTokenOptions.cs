namespace AuthService.Core.Authentication;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    public int ExpireMinutes { get; init; } = 10080; // 7 дней
    public int AccessTokenExpireMinutes { get; init; } = 15; // 15 минут
    public int TokenLengthBytes { get; init; } = 32;
    public string? Pepper { get; init; } // HMAC pepper (опционально)
}
