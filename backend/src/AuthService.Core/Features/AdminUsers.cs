using AuthService.Contracts;
using AuthService.Core.Authorization;
using AuthService.Domain.Accounts;
using AuthService.Domain.Roles;
using Core.Abstractions;
using Dapper;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Npgsql;

namespace AuthService.Core.Features;

public sealed class GetAdminUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/auth/admin/users", HandleAsync)
            .RequirePermissions(SystemPermissions.UsersView);

    private static async Task<Microsoft.AspNetCore.Http.IResult> HandleAsync(
        [FromServices] NpgsqlDataSource dataSource,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                a.id AS Id,
                COALESCE(a.email, '') AS Email,
                r.name AS Role
            FROM auth.accounts a
            LEFT JOIN auth.user_roles ur ON ur.user_id = a.id
            LEFT JOIN auth.roles r ON r.id = ur.role_id
            ORDER BY a.email, r.name;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<AdminUserRoleRow>(new CommandDefinition(sql, cancellationToken: ct));

        var users = rows
            .GroupBy(row => new { row.Id, row.Email })
            .Select(group => new AdminUserDto(
                group.Key.Id,
                group.Key.Email,
                group
                    .Select(row => row.Role)
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .Select(role => role!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(role => role)
                    .ToArray()))
            .ToArray();

        return Results.Ok(users);
    }

    private sealed record AdminUserRoleRow(Guid Id, string Email, string? Role);
}

public sealed class AssignAdminUserRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/admin/users/{accountId:guid}/roles/{role}", HandleAsync)
            .RequirePermissions(SystemPermissions.UsersManage);

    private static async Task<EndpointResult<EmptyResponse>> HandleAsync(
        Guid accountId,
        string role,
        [FromServices] AssignAdminUserRoleHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new AssignAdminUserRoleCommand(accountId, role), ct);
}

public sealed record AssignAdminUserRoleCommand(Guid AccountId, string Role) : ICommand;

public sealed class AssignAdminUserRoleHandler : ICommandHandler<EmptyResponse, AssignAdminUserRoleCommand>
{
    private readonly UserManager<Account> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public AssignAdminUserRoleHandler(UserManager<Account> userManager, RoleManager<Role> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<EmptyResponse, Error>> Handle(
        AssignAdminUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (!SystemRoles.Assignable.Contains(command.Role, StringComparer.OrdinalIgnoreCase))
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure("Role cannot be assigned"));

        var user = await _userManager.FindByIdAsync(command.AccountId.ToString());
        if (user is null)
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure("User not found"));

        if (!await _roleManager.RoleExistsAsync(command.Role))
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure("Role not found"));

        if (await _userManager.IsInRoleAsync(user, command.Role))
            return Result.Success<EmptyResponse, Error>(new EmptyResponse());

        var result = await _userManager.AddToRoleAsync(user, command.Role);
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(error => error.Description));
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure(message));
        }

        return Result.Success<EmptyResponse, Error>(new EmptyResponse());
    }
}

public sealed class RemoveAdminUserRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/auth/admin/users/{accountId:guid}/roles/{role}", HandleAsync)
            .RequirePermissions(SystemPermissions.UsersManage);

    private static async Task<EndpointResult<EmptyResponse>> HandleAsync(
        Guid accountId,
        string role,
        [FromServices] RemoveAdminUserRoleHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new RemoveAdminUserRoleCommand(accountId, role), ct);
}

public sealed record RemoveAdminUserRoleCommand(Guid AccountId, string Role) : ICommand;

public sealed class RemoveAdminUserRoleHandler : ICommandHandler<EmptyResponse, RemoveAdminUserRoleCommand>
{
    private readonly UserManager<Account> _userManager;

    public RemoveAdminUserRoleHandler(UserManager<Account> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<EmptyResponse, Error>> Handle(
        RemoveAdminUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (!SystemRoles.Assignable.Contains(command.Role, StringComparer.OrdinalIgnoreCase))
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure("Role cannot be removed"));

        var user = await _userManager.FindByIdAsync(command.AccountId.ToString());
        if (user is null)
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure("User not found"));

        if (!await _userManager.IsInRoleAsync(user, command.Role))
            return Result.Success<EmptyResponse, Error>(new EmptyResponse());

        if (command.Role.Equals(SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            var admins = await _userManager.GetUsersInRoleAsync(SystemRoles.Admin);
            if (admins.Count <= 1)
                return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure("Cannot remove the last admin"));
        }

        var result = await _userManager.RemoveFromRoleAsync(user, command.Role);
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(error => error.Description));
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure(message));
        }

        return Result.Success<EmptyResponse, Error>(new EmptyResponse());
    }
}
