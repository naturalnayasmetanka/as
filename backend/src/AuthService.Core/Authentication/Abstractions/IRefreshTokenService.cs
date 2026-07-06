namespace AuthService.Core.Authentication.Abstractions;

public interface IRefreshTokenService
{
    /// <summary>
    /// Генерирует новый refresh-токен.
    /// </summary>
    string GenerateToken();

    /// <summary>
    /// Вычисляет SHA-256 хеш токена.
    /// </summary>
    string ComputeTokenHash(string token);
}
