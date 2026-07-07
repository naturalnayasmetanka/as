namespace ProjectsService.Core.AuthService;

public sealed class AuthServiceClientOptions
{
    public const string SectionName = "AuthService";

    public string BaseAddress { get; init; } = string.Empty;
}
