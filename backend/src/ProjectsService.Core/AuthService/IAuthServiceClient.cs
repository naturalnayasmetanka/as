namespace ProjectsService.Core.AuthService;

public interface IAuthServiceClient
{
    Task<Result<AuthCurrentUser, AuthServiceClientError>> GetCurrentUserAsync(CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<AuthUser>, AuthServiceClientError>> GetUsersAsync(CancellationToken cancellationToken);
}
