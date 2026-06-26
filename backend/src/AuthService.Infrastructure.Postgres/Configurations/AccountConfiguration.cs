using AuthService.Domain.Accounts;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Postgres.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256);
        builder.Property(a => a.EmailConfirmed).HasColumnName("email_confirmed").IsRequired();
        builder.Property(a => a.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(a => a.SecurityStamp).HasColumnName("security_stamp");
        builder.Property(a => a.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
        builder.Property(a => a.PhoneNumber).HasColumnName("phone_number");
        builder.Property(a => a.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
        builder.Property(a => a.TwoFactorEnabled).HasColumnName("two_factor_enabled");
        builder.Property(a => a.LockoutEnd).HasColumnName("lockout_end");
        builder.Property(a => a.LockoutEnabled).HasColumnName("lockout_enabled").IsRequired();
        builder.Property(a => a.AccessFailedCount).HasColumnName("access_failed_count");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
