using AuthService.Core.Authorization;
using AuthService.Domain.Accounts;
using AuthService.Domain.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Shared.Authorization;

namespace AuthService.Infrastructure.Postgres.Authorization;

public sealed class SystemRoleSeeder
{
    private static readonly SeedUser[] TestUsers =
    [
        new("user@example.com", "User123!", SystemRoles.User),
        new("employee@example.com", "Employee123!", SystemRoles.Employee),
        new("moderator@example.com", "Moderator123!", SystemRoles.Moderator)
    ];

    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<Account> _userManager;
    private readonly BootstrapAdminOptions _options;

    public SystemRoleSeeder(
        RoleManager<Role> roleManager,
        UserManager<Account> userManager,
        IOptions<BootstrapAdminOptions> options)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task SeedAsync()
    {
        foreach (var roleName in SystemRoles.All)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new Role(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed role '{roleName}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        foreach (var user in TestUsers)
        {
            await EnsureUserWithRoleAsync(user.Email, user.Password, user.Role);
        }

        if (string.IsNullOrWhiteSpace(_options.Email))
        {
            return;
        }

        var admin = await _userManager.FindByEmailAsync(_options.Email);
        if (admin is null)
        {
            if (string.IsNullOrWhiteSpace(_options.Password))
            {
                return;
            }

            admin = new Account(_options.Email);
            var createResult = await _userManager.CreateAsync(admin, _options.Password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create bootstrap admin '{_options.Email}': {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        await EnsureRoleAsync(admin, SystemRoles.User);
        await EnsureRoleAsync(admin, SystemRoles.Admin);
    }

    private async Task EnsureUserWithRoleAsync(string email, string password, string role)
    {
        var account = await _userManager.FindByEmailAsync(email);
        if (account is null)
        {
            account = new Account(email);
            var createResult = await _userManager.CreateAsync(account, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create test user '{email}': {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        await EnsureRoleAsync(account, SystemRoles.User);

        if (!string.Equals(role, SystemRoles.User, StringComparison.Ordinal))
        {
            await EnsureRoleAsync(account, role);
        }
    }

    private async Task EnsureRoleAsync(Account account, string role)
    {
        if (await _userManager.IsInRoleAsync(account, role))
        {
            return;
        }

        var addResult = await _userManager.AddToRoleAsync(account, role);
        if (!addResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add role '{role}' to bootstrap admin '{account.Email}': {string.Join("; ", addResult.Errors.Select(e => e.Description))}");
        }
    }

    private sealed record SeedUser(string Email, string Password, string Role);
}
