using ProjectsService.Domain.Projects;

namespace ProjectsService.Infrastructure.Postgres;

public sealed class ProjectsServiceDbContext : DbContext
{
    public ProjectsServiceDbContext(DbContextOptions<ProjectsServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasDefaultSchema("projects");
        builder.ApplyConfigurationsFromAssembly(typeof(ProjectsServiceDbContext).Assembly);
    }
}
