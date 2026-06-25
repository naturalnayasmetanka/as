using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Accounts;

public sealed class Account : IdentityUser<Guid>
{
    private Account() { }

    public Account(string email, string passwordHash, bool lockoutEnabled)
    {
        Id = Guid.CreateVersion7();
        Email = email;
        PasswordHash = passwordHash;
        LockoutEnabled = lockoutEnabled;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

    }

    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }
}
