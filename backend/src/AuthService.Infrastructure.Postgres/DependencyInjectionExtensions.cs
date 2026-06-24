using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuthService.Core.Database;
using AuthService.Infrastructure.Postgres.Repositories;

namespace AuthService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructurePostgres(
        this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");

        services.AddDbContextPool<AuthServiceDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IWidgetsRepository, WidgetsRepository>();

        return services;
    }
}
