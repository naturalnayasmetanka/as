using AuthService.Domain.Accounts;
using AuthService.Domain.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Postgres.Identity;

public sealed class IdentityBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IdentityBootstrapOptions _options;

    public IdentityBootstrapHostedService(
        IServiceProvider serviceProvider,
        IOptions<IdentityBootstrapOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();

        foreach (var roleName in SystemRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(new Role(roleName));
            if (!result.Succeeded)
            {
                var message = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed role '{roleName}': {message}");
            }
        }

        if (string.IsNullOrWhiteSpace(_options.AdminEmail))
            return;

        var admin = await userManager.FindByEmailAsync(_options.AdminEmail);
        if (admin is null || await userManager.IsInRoleAsync(admin, SystemRoles.Admin))
            return;

        var addAdminResult = await userManager.AddToRoleAsync(admin, SystemRoles.Admin);
        if (!addAdminResult.Succeeded)
        {
            var message = string.Join("; ", addAdminResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to bootstrap admin '{_options.AdminEmail}': {message}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
