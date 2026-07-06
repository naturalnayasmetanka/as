namespace AuthService.Domain.RefreshSessions;

public sealed class RefreshSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RotatedAt { get; set; }
    public Guid? ParentSessionId { get; set; }

    public RefreshSession() { }

    public static RefreshSession Create(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        Guid? parentSessionId = null)
    {
        return new RefreshSession
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow,
            RotatedAt = null,
            ParentSessionId = parentSessionId
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
    }
}
