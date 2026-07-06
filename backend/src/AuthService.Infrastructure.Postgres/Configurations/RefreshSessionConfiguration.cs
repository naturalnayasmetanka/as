using AuthService.Domain.RefreshSessions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Postgres.Configurations;

internal sealed class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("refresh_sessions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(r => r.UserId).HasColumnName("user_id");
        builder.Property(r => r.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
        builder.Property(r => r.ExpiresAt).HasColumnName("expires_at");
        builder.Property(r => r.IsRevoked).HasColumnName("is_revoked").HasDefaultValue(false);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.RotatedAt).HasColumnName("rotated_at");
        builder.Property(r => r.ParentSessionId).HasColumnName("parent_session_id");

        builder.HasIndex(r => r.UserId).HasDatabaseName("idx_refresh_sessions_user_id");
        builder.HasIndex(r => r.TokenHash).HasDatabaseName("idx_refresh_sessions_token_hash");
        builder.HasIndex(r => r.ExpiresAt).HasDatabaseName("idx_refresh_sessions_expires_at");
    }
}
