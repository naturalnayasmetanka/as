using AuthService.Contracts;
using AuthService.Domain.Accounts;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Authorization;

namespace AuthService.Core.Features.Widgets.UseCases;

public sealed record CreateUserCommand(string email, string password) : ICommand;

public sealed class CreateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/register", HandleAsync);

    private static async Task<EndpointResult<CreateUserResponse>> HandleAsync(
        [FromBody] CreateUserRequest request,
        [FromServices] CreateUserHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new CreateUserCommand(request.Email, request.Password), ct);
}

public sealed class CreateUserHandler : ICommandHandler<CreateUserResponse, CreateUserCommand>
{
    private readonly UserManager<Account> _userManager;

    public CreateUserHandler(UserManager<Account> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<CreateUserResponse, Error>> Handle(
       CreateUserCommand command,
       CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByEmailAsync(command.email);
        if (existing is not null)
            return Result.Failure<CreateUserResponse, Error>(GeneralErrors.Failure("User alreadu exists"));

        var account = new Account(command.email);

        var result = await _userManager.CreateAsync(account, command.password);

        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<CreateUserResponse, Error>(GeneralErrors.Failure(message));
        }

        var roleResult = await _userManager.AddToRoleAsync(account, SystemRoles.User);
        if (!roleResult.Succeeded)
        {
            var message = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            return Result.Failure<CreateUserResponse, Error>(GeneralErrors.Failure(message));
        }

        return Result.Success<CreateUserResponse, Error>(new CreateUserResponse(account.Id));
    }
}
