using AuthService.Core.Authentication;
using AuthService.Core.Database;
using AuthService.Domain.Accounts;
using AuthService.Domain.Roles;


//using AuthService.Infrastructure.Postgres.Repositories;
using Dapper;
using Microsoft.AspNetCore.Identity;
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

        //services.AddScoped<IWidgetsRepository, WidgetsRepository>();

        services.AddScoped<ITransactionManager, TransactionManager>();

        services.AddIdentity(configuration);

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

    private static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
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
        })
        .AddEntityFrameworkStores<AuthServiceDbContext>()
        .AddDefaultTokenProviders();

        services
            .AddAuthentication()
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyDefaults.AUTHENTICATION_SCHEME,
                options => configuration.GetSection("ApiKey").Bind(options));

        services.AddAuthorization();

        return services;
    }
}
