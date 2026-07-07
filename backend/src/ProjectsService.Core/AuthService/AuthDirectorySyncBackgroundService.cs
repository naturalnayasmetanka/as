using Microsoft.Extensions.Hosting;

namespace ProjectsService.Core.AuthService;

public sealed class AuthDirectorySyncBackgroundService : BackgroundService
{
    private readonly IAuthServiceClient _authServiceClient;
    private readonly ILogger<AuthDirectorySyncBackgroundService> _logger;

    public AuthDirectorySyncBackgroundService(
        IAuthServiceClient authServiceClient,
        ILogger<AuthDirectorySyncBackgroundService> logger)
    {
        _authServiceClient = authServiceClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            var usersResult = await _authServiceClient.GetUsersAsync(stoppingToken);
            if (usersResult.IsFailure)
            {
                _logger.LogWarning(
                    "AuthService directory sync failed with status {StatusCode}: {Message}",
                    (int)usersResult.Error.StatusCode,
                    usersResult.Error.Message);
                return;
            }

            _logger.LogInformation(
                "AuthService directory sync completed. Users read: {UserCount}",
                usersResult.Value.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AuthService directory sync failed.");
        }
    }
}
