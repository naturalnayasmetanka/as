using AuthService.Core.Authentication;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AuthService.Core.Features;

public sealed record GetUsersCommand() : ICommand;


public sealed class GetUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/users", HandleAsync);

    [Authorize(AuthenticationSchemes = ApiKeyDefaults.AUTHENTICATION_SCHEME)]
    private static async Task<EndpointResult<Guid>> HandleAsync(
        [FromServices] GetUsersHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new GetUsersCommand(), ct);
}

public sealed class GetUsersHandler : ICommandHandler<Guid, GetUsersCommand>
{

    public GetUsersHandler()
    {

    }

    public async Task<Result<Guid, Error>> Handle(GetUsersCommand command, CancellationToken cancellationToken)
    {
        return Result.Success<Guid, Error>(Guid.CreateVersion7());
    }
}