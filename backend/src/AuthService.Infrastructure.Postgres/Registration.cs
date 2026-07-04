using AuthService.Core.Authentication;
using AuthService.Core.Database;
using AuthService.Domain.Accounts;
using AuthService.Domain.Roles;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace AuthService.Infrastructure.Postgres;

public static class Registration
{
    private const string AUTH_DB_CONNECTION_NAME = "Db";

    public static IServiceCollection AddInfrastructure(
       this IServiceCollection services,
       IConfiguration configuration,
       IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddDbContext(configuration, environment);

        services.AddScoped<ITransactionManager, TransactionManager>();

        services.AddIdentity(configuration, environment);

        return services;
    }

    private static IServiceCollection AddDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        bool isDevelopment = environment.IsDevelopment();

        string connectionString = configuration.GetConnectionString(AUTH_DB_CONNECTION_NAME)
                                  ?? throw new InvalidOperationException(
                                      $"Connection string '{AUTH_DB_CONNECTION_NAME}' is not configured.");

        NpgsqlDataSource dataSource =
            new NpgsqlDataSourceBuilder(connectionString) { Name = AUTH_DB_CONNECTION_NAME, }.Build();

        services.AddSingleton(dataSource);

        services.AddDbContextPool<AuthServiceDbContext>(options =>
        {
            options.UseNpgsql(dataSource, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "auth"));

            if (isDevelopment)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    private static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddIdentity<Account, Role>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;

            options.ClaimsIdentity.UserIdClaimType = "sub";
            options.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;
            options.ClaimsIdentity.EmailClaimType = ClaimTypes.Email;
            options.ClaimsIdentity.SecurityStampClaimType = "security_stamp";
        })
        .AddEntityFrameworkStores<AuthServiceDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Name = "AuthService.Identity";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;

            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.None
                : CookieSecurePolicy.Always;

            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAuthentication();

        services.AddAuthorization();

        return services;
    }
}
