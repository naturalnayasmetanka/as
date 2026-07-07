namespace Shared.Authorization;

public static class SystemRoles
{
    public const string User = nameof(User);
    public const string Employee = nameof(Employee);
    public const string Moderator = nameof(Moderator);
    public const string Admin = nameof(Admin);
    public const string ServiceAccount = nameof(ServiceAccount);

    public static readonly string[] All = [User, Employee, Moderator, Admin, ServiceAccount];

    public static readonly string[] Assignable = [Employee, Moderator, Admin];
}
