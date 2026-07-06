using AuthService.Core.Database.Abstractions;
using AuthService.Domain.RefreshSessions;

namespace AuthService.Infrastructure.Postgres.Repositories;

internal sealed class RefreshSessionRepository : IRefreshSessionRepository
{
    private readonly AuthServiceDbContext _context;

    public RefreshSessionRepository(AuthServiceDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshSession> CreateAsync(RefreshSession session, CancellationToken cancellationToken)
    {
        _context.RefreshSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<RefreshSession?> FindValidByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        return await _context.RefreshSessions
            .FirstOrDefaultAsync(
                rs => rs.TokenHash == tokenHash
                    && !rs.IsRevoked
                    && rs.ExpiresAt > utcNow,
                cancellationToken);
    }

    public async Task<RefreshSession?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _context.RefreshSessions
            .FirstOrDefaultAsync(rs => rs.Id == sessionId, cancellationToken);
    }

    public async Task UpdateAsync(RefreshSession session, CancellationToken cancellationToken)
    {
        _context.RefreshSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeSessionChainAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // Находим сессию, которая была скомпрометирована
        var session = await _context.RefreshSessions.FindAsync(new object?[] { sessionId }, cancellationToken: cancellationToken);
        if (session is null)
            return;

        // Находим корневую сессию (идём вверх по цепочке родителей)
        var rootSessionId = session.Id;
        var current = session;
        while (current.ParentSessionId.HasValue)
        {
            var parent = await _context.RefreshSessions.FindAsync(new object?[] { current.ParentSessionId }, cancellationToken: cancellationToken);
            if (parent is null) break;
            rootSessionId = parent.Id;
            current = parent;
        }

        // Отзываем эту сессию и все её потомки
        // Используем простой подход: отзываем все сессии, которые имеют этот rootSessionId как предка или сам он
        var sessionsToRevoke = await _context.RefreshSessions
            .Where(rs => rs.Id == rootSessionId)
            .ToListAsync(cancellationToken);

        // Также найдём все потомки (с ParentSessionId == rootSessionId или идущие от rootSessionId)
        var allSessions = await _context.RefreshSessions.ToListAsync(cancellationToken);
        var toRevoke = new HashSet<Guid> { rootSessionId };

        // BFS для поиска всех потомков
        var queue = new Queue<Guid>();
        queue.Enqueue(rootSessionId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = allSessions.Where(rs => rs.ParentSessionId == currentId).ToList();

            foreach (var child in children)
            {
                if (!toRevoke.Contains(child.Id))
                {
                    toRevoke.Add(child.Id);
                    queue.Enqueue(child.Id);
                }
            }
        }

        // Отзываем все найденные сессии
        foreach (var s in allSessions.Where(rs => toRevoke.Contains(rs.Id)))
        {
            s.Revoke();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _context.RefreshSessions.FindAsync(new object?[] { sessionId }, cancellationToken: cancellationToken);
        if (session is not null)
        {
            session.Revoke();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        await _context.RefreshSessions
            .Where(rs => rs.ExpiresAt <= utcNow)
            .ExecuteDeleteAsync(cancellationToken);
    }

}
