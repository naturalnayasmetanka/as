using AuthService.Domain.Accounts;
using AuthService.Domain.RefreshSessions;
using AuthService.Domain.Roles;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AuthService.Infrastructure.Postgres;

public sealed class AuthServiceDbContext : IdentityDbContext<Account, Role, Guid>
{
    public AuthServiceDbContext(DbContextOptions<AuthServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshSession> RefreshSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        builder.HasDefaultSchema("auth");
        builder.ApplyConfigurationsFromAssembly(typeof(AuthServiceDbContext).Assembly);
    }
}
