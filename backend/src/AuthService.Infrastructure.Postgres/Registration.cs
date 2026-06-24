using AuthService.Core.Database;
using AuthService.Infrastructure.Postgres.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace AuthService.Infrastructure.Postgres;

public static class Registration
{
    public const string AUTH_DB_CONNECTION_NAME = "Db";

    public static IServiceCollection AddInfrastructure(
       this IServiceCollection services,
       IConfiguration configuration,
       IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

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

        services.AddScoped<IWidgetsRepository, WidgetsRepository>();

        services.AddScoped<ITransactionManager, TransactionManager>();

        return services;
    }
}
