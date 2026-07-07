using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectsService.Domain.Projects;

namespace ProjectsService.Infrastructure.Postgres.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.OwnerId).HasColumnName("owner_id");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(160);
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => p.OwnerId).HasDatabaseName("idx_projects_owner_id");
        builder.HasIndex(p => p.Status).HasDatabaseName("idx_projects_status");
    }
}
