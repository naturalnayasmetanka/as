using AuthService.Contracts;

namespace AuthService.Core.Database.Abstractions;

public interface IAdminUserReadRepository
{
    Task<IReadOnlyCollection<AdminUserResponse>> GetUsersWithRolesAsync(CancellationToken cancellationToken);
}
