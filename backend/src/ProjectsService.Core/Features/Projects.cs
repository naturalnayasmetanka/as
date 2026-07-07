using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ProjectsService.Core.Authorization;
using ProjectsService.Core.Database.Abstractions;
using ProjectsService.Domain.Projects;
using Shared.Authorization;

namespace ProjectsService.Core.Features;

public sealed record ProjectResponse(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Description,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateProjectRequest(string Name, string Description);

public sealed record UpdateProjectRequest(string Name, string Description, string Status);

public sealed record GetProjectsCommand : ICommand;

public sealed record GetProjectCommand(Guid ProjectId) : ICommand;

public sealed record CreateProjectCommand(string Name, string Description) : ICommand;

public sealed record UpdateProjectCommand(Guid ProjectId, string Name, string Description, string Status) : ICommand;

public sealed record DeleteProjectCommand(Guid ProjectId) : ICommand;

public sealed record EmptyResponse;

public sealed class ProjectsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/projects", GetProjectsAsync)
            .RequirePermissions(ProjectsPermissions.ProjectsView);

        app.MapGet("/projects/{projectId:guid}", GetProjectAsync)
            .RequirePermissions(ProjectsPermissions.ProjectsView);

        app.MapPost("/projects", CreateProjectAsync)
            .RequirePermissions(ProjectsPermissions.ProjectsManage);

        app.MapPut("/projects/{projectId:guid}", UpdateProjectAsync)
            .RequirePermissions(ProjectsPermissions.ProjectsManage);

        app.MapDelete("/projects/{projectId:guid}", DeleteProjectAsync)
            .RequirePermissions(ProjectsPermissions.ProjectsManage);
    }

    private static async Task<EndpointResult<IReadOnlyCollection<ProjectResponse>>> GetProjectsAsync(
        [FromServices] GetProjectsHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new GetProjectsCommand(), ct);

    private static async Task<EndpointResult<ProjectResponse>> GetProjectAsync(
        Guid projectId,
        [FromServices] GetProjectHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new GetProjectCommand(projectId), ct);

    private static async Task<EndpointResult<ProjectResponse>> CreateProjectAsync(
        [FromBody] CreateProjectRequest request,
        [FromServices] CreateProjectHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new CreateProjectCommand(request.Name, request.Description), ct);

    private static async Task<EndpointResult<ProjectResponse>> UpdateProjectAsync(
        Guid projectId,
        [FromBody] UpdateProjectRequest request,
        [FromServices] UpdateProjectHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new UpdateProjectCommand(projectId, request.Name, request.Description, request.Status), ct);

    private static async Task<EndpointResult<EmptyResponse>> DeleteProjectAsync(
        Guid projectId,
        [FromServices] DeleteProjectHandler handler,
        CancellationToken ct) =>
        await handler.Handle(new DeleteProjectCommand(projectId), ct);
}

public sealed class GetProjectsHandler : ICommandHandler<IReadOnlyCollection<ProjectResponse>, GetProjectsCommand>
{
    private readonly IProjectRepository _repository;
    private readonly CurrentUser _currentUser;

    public GetProjectsHandler(IProjectRepository repository, CurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<ProjectResponse>, Error>> Handle(
        GetProjectsCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
        {
            return Result.Failure<IReadOnlyCollection<ProjectResponse>, Error>(GeneralErrors.Failure("User not found in token"));
        }

        var projects = await _repository.GetForUserAsync(
            userId,
            _currentUser.Roles.Contains(SystemRoles.Admin),
            cancellationToken);

        return Result.Success<IReadOnlyCollection<ProjectResponse>, Error>(projects.Select(ToResponse).ToArray());
    }

    private static ProjectResponse ToResponse(Project project) =>
        new(project.Id, project.OwnerId, project.Name, project.Description, project.Status.ToString(), project.CreatedAt, project.UpdatedAt);
}

public sealed class GetProjectHandler : ICommandHandler<ProjectResponse, GetProjectCommand>
{
    private readonly IProjectRepository _repository;
    private readonly CurrentUser _currentUser;

    public GetProjectHandler(IProjectRepository repository, CurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<ProjectResponse, Error>> Handle(
        GetProjectCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
        {
            return Result.Failure<ProjectResponse, Error>(GeneralErrors.Failure("User not found in token"));
        }

        var project = await _repository.GetByIdAsync(
            command.ProjectId,
            userId,
            _currentUser.Roles.Contains(SystemRoles.Admin),
            cancellationToken);

        if (project is null)
        {
            return Result.Failure<ProjectResponse, Error>(GeneralErrors.Failure("Project not found"));
        }

        return Result.Success<ProjectResponse, Error>(
            new ProjectResponse(project.Id, project.OwnerId, project.Name, project.Description, project.Status.ToString(), project.CreatedAt, project.UpdatedAt));
    }
}

public sealed class CreateProjectHandler : ICommandHandler<ProjectResponse, CreateProjectCommand>
{
    private readonly IProjectRepository _repository;
    private readonly CurrentUser _currentUser;

    public CreateProjectHandler(IProjectRepository repository, CurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<ProjectResponse, Error>> Handle(
        CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
        {
            return Result.Failure<ProjectResponse, Error>(GeneralErrors.Failure("User not found in token"));
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<ProjectResponse, Error>(GeneralErrors.Failure("Project name is required"));
        }

        var project = Project.Create(userId, command.Name, command.Description);
        await _repository.AddAsync(project, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success<ProjectResponse, Error>(
            new ProjectResponse(project.Id, project.OwnerId, project.Name, project.Description, project.Status.ToString(), project.CreatedAt, project.UpdatedAt));
    }
}

public sealed class UpdateProjectHandler : ICommandHandler<ProjectResponse, UpdateProjectCommand>
{
    private readonly IProjectRepository _repository;
    private readonly CurrentUser _currentUser;

    public UpdateProjectHandler(IProjectRepository repository, CurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<ProjectResponse, Error>> Handle(
        UpdateProjectCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
        {
            return Result.Failure<ProjectResponse, Error>(GeneralErrors.Failure("User not found in token"));
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<ProjectResponse, Error>(GeneralErrors.Failure("Project name is required"));
        }

        var project = await _repository.GetByIdAsync(
            command.ProjectId,
            userId,
            _currentUser.Roles.Contains(SystemRoles.Admin),
            cancellationToken);

        if (project is null)
        {
            return Result.Failure<ProjectResponse, Error>(GeneralErrors.Failure("Project not found"));
        }

        if (!Enum.TryParse<ProjectStatus>(command.Status, ignoreCase: true, out var status))
        {
            return Result.Failure<ProjectResponse, Error>(GeneralErrors.Failure("Project status is invalid"));
        }

        project.Update(command.Name, command.Description, status);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success<ProjectResponse, Error>(
            new ProjectResponse(project.Id, project.OwnerId, project.Name, project.Description, project.Status.ToString(), project.CreatedAt, project.UpdatedAt));
    }
}

public sealed class DeleteProjectHandler : ICommandHandler<EmptyResponse, DeleteProjectCommand>
{
    private readonly IProjectRepository _repository;
    private readonly CurrentUser _currentUser;

    public DeleteProjectHandler(IProjectRepository repository, CurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<EmptyResponse, Error>> Handle(
        DeleteProjectCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
        {
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure("User not found in token"));
        }

        var project = await _repository.GetByIdAsync(
            command.ProjectId,
            userId,
            _currentUser.Roles.Contains(SystemRoles.Admin),
            cancellationToken);

        if (project is null)
        {
            return Result.Failure<EmptyResponse, Error>(GeneralErrors.Failure("Project not found"));
        }

        _repository.Delete(project);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success<EmptyResponse, Error>(new EmptyResponse());
    }
}
