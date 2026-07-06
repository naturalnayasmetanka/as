namespace AuthService.Infrastructure.Postgres.Identity;

public sealed class IdentityBootstrapOptions
{
    public const string SectionName = "AuthBootstrap";

    public string? AdminEmail { get; set; }
}
