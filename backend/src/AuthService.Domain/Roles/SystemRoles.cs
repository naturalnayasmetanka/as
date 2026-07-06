namespace AuthService.Domain.Roles;

public static class SystemRoles
{
    public const string User = "User";
    public const string Employee = "Employee";
    public const string Moderator = "Moderator";
    public const string Admin = "Admin";

    public static readonly IReadOnlyCollection<string> All =
    [
        User,
        Employee,
        Moderator,
        Admin
    ];

    public static readonly IReadOnlyCollection<string> Assignable =
    [
        Employee,
        Moderator,
        Admin
    ];
}
