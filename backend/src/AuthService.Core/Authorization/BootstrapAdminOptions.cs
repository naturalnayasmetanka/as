namespace AuthService.Core.Authorization;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string? Email { get; init; }

    public string? Password { get; init; }
}
