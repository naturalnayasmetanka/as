using AuthService.Contracts;
using AuthService.Core.Authorization;
using AuthService.Core.Database.Abstractions;
using AuthService.Domain.Accounts;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AuthService.Core.Features;

public sealed record GetAdminUsersCommand : ICommand;

public sealed record AssignUserRoleCommand(Guid AccountId, string Role) : ICommand;

public sealed class AdminUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/admin/users", GetUsersAsync)
            .RequirePermissions(Permissions.UsersView);

        app.MapPost("/auth/admin/users/{accountId:guid}/roles/{role}", AssignRoleAsync)
            .RequirePermissions(Permissions.UsersManage);
    }

    private static async Task<EndpointResult<IReadOnlyCollection<AdminUserResponse>>> GetUsersAsync(
        [FromServices] GetAdminUsersHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new GetAdminUsersCommand(), ct);

    private static async Task<EndpointResult<EmptyResponse>> AssignRoleAsync(
        Guid accountId,
        string role,
        [FromServices] AssignUserRoleHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new AssignUserRoleCommand(accountId, role), ct);
}

public sealed class GetAdminUsersHandler : ICommandHandler<IReadOnlyCollection<AdminUserResponse>, GetAdminUsersCommand>
{
    private readonly IAdminUserReadRepository _repository;

    public GetAdminUsersHandler(IAdminUserReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyCollection<AdminUserResponse>, Error>> Handle(
        GetAdminUsersCommand command,
        CancellationToken cancellationToken)
    {
        var users = await _repository.GetUsersWithRolesAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<AdminUserResponse>, Error>(users);
    }
}

public sealed class AssignUserRoleHandler : ICommandHandler<EmptyResponse, AssignUserRoleCommand>
{
    private readonly UserManager<Account> _userManager;
    private readonly RoleManager<Domain.Roles.Role> _roleManager;

    public AssignUserRoleHandler(
        UserManager<Account> userManager,
        RoleManager<Domain.Roles.Role> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<EmptyResponse, Error>> Handle(
        AssignUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (!SystemRoles.Assignable.Contains(command.Role, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<EmptyResponse, Error>(
                GeneralErrors.Failure("Role cannot be assigned"));
        }

        var user = await _userManager.FindByIdAsync(command.AccountId.ToString());
        if (user is null)
        {
            return Result.Failure<EmptyResponse, Error>(
                GeneralErrors.Failure("User not found"));
        }

        var role = await _roleManager.FindByNameAsync(command.Role);
        if (role is null || string.IsNullOrWhiteSpace(role.Name))
        {
            return Result.Failure<EmptyResponse, Error>(
                GeneralErrors.Failure("Role not found"));
        }

        if (await _userManager.IsInRoleAsync(user, role.Name))
        {
            return Result.Success<EmptyResponse, Error>(new EmptyResponse());
        }

        var result = await _userManager.AddToRoleAsync(user, role.Name);
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure(message));
        }

        return Result.Success<EmptyResponse, Error>(new EmptyResponse());
    }
}
