using AuthService.Contracts;
using AuthService.Core.Database.Abstractions;
using Dapper;
using Npgsql;

namespace AuthService.Infrastructure.Postgres.Repositories;

public sealed class AdminUserReadRepository : IAdminUserReadRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public AdminUserReadRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyCollection<AdminUserResponse>> GetUsersWithRolesAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                a.id,
                a."Email" as email,
                coalesce(array_agg(r.name order by r.name) filter (where r.name is not null), '{}') as roles
            from auth.accounts a
            left join auth.user_roles ur on ur.user_id = a.id
            left join auth.roles r on r.id = ur.role_id
            group by a.id, a."Email"
            order by a."Email";
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var users = await connection.QueryAsync<AdminUserRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return users
            .Select(user => new AdminUserResponse(user.Id, user.Email, user.Roles ?? []))
            .ToArray();
    }

    private sealed class AdminUserRow
    {
        public Guid Id { get; init; }

        public string Email { get; init; } = string.Empty;

        public string[]? Roles { get; init; }
    }
}
