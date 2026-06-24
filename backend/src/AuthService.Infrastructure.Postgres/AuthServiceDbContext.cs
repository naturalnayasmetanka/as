namespace AuthService.Infrastructure.Postgres;

public sealed class AuthServiceDbContext : DbContext
{
    public AuthServiceDbContext(DbContextOptions<AuthServiceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("auth");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthServiceDbContext).Assembly);
    }
}
