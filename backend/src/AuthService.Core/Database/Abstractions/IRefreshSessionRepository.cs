using AuthService.Domain.RefreshSessions;

namespace AuthService.Core.Database.Abstractions;

public interface IRefreshSessionRepository
{
    /// <summary>
    /// Создает новую refresh-сессию.
    /// </summary>
    Task<RefreshSession> CreateAsync(RefreshSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Находит сессию по хешу токена, проверяя что она не истекла и не отозвана.
    /// </summary>
    Task<RefreshSession?> FindValidByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Находит сессию по ID.
    /// </summary>
    Task<RefreshSession?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Обновляет сессию (для ротации).
    /// </summary>
    Task UpdateAsync(RefreshSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Отзывает всю цепочку сессий начиная с текущей (для reuse detection).
    /// </summary>
    Task RevokeSessionChainAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Отзывает конкретную сессию.
    /// </summary>
    Task RevokeAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Удаляет истекшие интокены.
    /// </summary>
    Task DeleteExpiredAsync(CancellationToken cancellationToken);
}
