using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Accounts;

public sealed class Account : IdentityUser<Guid>
{
    public Account(string email)
    {
        Id = Guid.CreateVersion7();
        UserName = email;
        Email = email;
        LockoutEnabled = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

    }

    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }
}
