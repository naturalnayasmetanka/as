namespace ProjectsService.Core.AuthService;

public interface IServiceTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
