using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ProjectsService.Core.AuthService;
using ProjectsService.Core.Authorization;
using ProjectsService.Core.Database.Abstractions;
using Shared.Authorization;
using System.Net;

namespace ProjectsService.Core.Features;

public sealed record ProjectOwnerReportItem(
    Guid ProjectId,
    string ProjectName,
    Guid OwnerId,
    string OwnerEmail,
    string Status);

public sealed record ProjectOwnerReportResponse(IReadOnlyCollection<ProjectOwnerReportItem> Projects);

public sealed record GetProjectOwnerReportCommand : ICommand;

public sealed class ProjectOwnerReportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/projects/report/owners", GetReportAsync)
            .RequirePermissions(ProjectsPermissions.ProjectsView);
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> GetReportAsync(
        [FromServices] GetProjectOwnerReportHandler handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(new GetProjectOwnerReportCommand(), ct);

        if (result.IsSuccess)
        {
            return Results.Ok(new { result = result.Value, isError = false });
        }

        return result.Error.StatusCode switch
        {
            HttpStatusCode.Unauthorized => Results.Unauthorized(),
            HttpStatusCode.Forbidden => Results.Forbid(),
            _ => Results.Problem(
                title: "AuthService request failed",
                detail: result.Error.Message,
                statusCode: StatusCodes.Status502BadGateway)
        };
    }
}

public sealed class GetProjectOwnerReportHandler
{
    private readonly IProjectRepository _repository;
    private readonly CurrentUser _currentUser;
    private readonly IAuthServiceClient _authServiceClient;

    public GetProjectOwnerReportHandler(
        IProjectRepository repository,
        CurrentUser currentUser,
        IAuthServiceClient authServiceClient)
    {
        _repository = repository;
        _currentUser = currentUser;
        _authServiceClient = authServiceClient;
    }

    public async Task<Result<ProjectOwnerReportResponse, AuthServiceClientError>> Handle(
        GetProjectOwnerReportCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
        {
            return Result.Failure<ProjectOwnerReportResponse, AuthServiceClientError>(
                new AuthServiceClientError(HttpStatusCode.Unauthorized, "User not found in token"));
        }

        var projects = await _repository.GetForUserAsync(
            userId,
            _currentUser.Roles.Contains(SystemRoles.Admin),
            cancellationToken);

        var usersResult = await _authServiceClient.GetUsersAsync(cancellationToken);
        if (usersResult.IsFailure)
        {
            return Result.Failure<ProjectOwnerReportResponse, AuthServiceClientError>(usersResult.Error);
        }

        var usersById = usersResult.Value.ToDictionary(user => user.Id);
        var items = projects
            .Select(project =>
            {
                var ownerEmail = usersById.TryGetValue(project.OwnerId, out var owner)
                    ? owner.Email
                    : "unknown";

                return new ProjectOwnerReportItem(
                    project.Id,
                    project.Name,
                    project.OwnerId,
                    ownerEmail,
                    project.Status.ToString());
            })
            .ToArray();

        return Result.Success<ProjectOwnerReportResponse, AuthServiceClientError>(
            new ProjectOwnerReportResponse(items));
    }
}
