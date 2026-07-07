using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Roles;

public sealed class Role : IdentityRole<Guid>
{
    public Role()
    {
        Id = Guid.CreateVersion7();
    }

    public Role(string name)
        : this()
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
    }
}
