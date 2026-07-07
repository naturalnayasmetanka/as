using AuthService.Core;
using AuthService.Infrastructure.Postgres;
using AuthService.Infrastructure.Postgres.Authorization;
using Framework.Endpoints;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Shared.Authorization;
using CoreRegistration = AuthService.Core.Registration;

namespace AuthService.Web;

public static class Program
{
    private const string LocalFrontendCorsPolicy = "LocalFrontend";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddLocalDotEnv(builder.Environment);

        builder.Services
            .AddCore(builder.Configuration)
            .AddInfrastructure(builder.Configuration, builder.Environment);

        builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(LocalFrontendCorsPolicy, policy =>
            {
                policy
                    .WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        var app = builder.Build();

        var coreAssembly = typeof(CoreRegistration).Assembly;
        var endpointTypes = coreAssembly.GetTypes().Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        foreach (var type in endpointTypes)
        {
            if (Activator.CreateInstance(type) is IEndpoint endpoint)
            {
                endpoint.MapEndpoint(app);
            }
        }

        app.MapHealthChecks("/health");

        app.UseCors(LocalFrontendCorsPolicy);
        app.UseAuthentication();
        app.UseCurrentUser();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(opt =>
            {
                opt.RouteTemplate = "openapi/{documentName}.json";
            });
            app.UseStaticFiles();
            app.MapScalarApiReference(opt =>
            {
                opt.Title = "Scalar Example";
                opt.Theme = ScalarTheme.Mars;
                opt.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.Http11);
            });
        }

        app.UseHttpsRedirection();

        app.MapEndpoints();

        ApplyDevelopmentMigrations(app);

        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<SystemRoleSeeder>();
            seeder.SeedAsync().GetAwaiter().GetResult();
        }

        app.Run();
    }

    private static void ApplyDevelopmentMigrations(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthServiceDbContext>();
        dbContext.Database.Migrate();
    }

    private static void AddLocalDotEnv(this IConfigurationBuilder configuration, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var envPath = FindFileUpwards(environment.ContentRootPath, ".env");
        if (envPath is null)
        {
            return;
        }

        var values = File
            .ReadAllLines(envPath)
            .Select(ParseEnvLine)
            .Where(pair => pair is not null)
            .ToDictionary(pair => pair!.Value.Key, pair => pair!.Value.Value, StringComparer.OrdinalIgnoreCase);

        configuration.AddInMemoryCollection(values);
    }

    private static string? FindFileUpwards(string startPath, string fileName)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static KeyValuePair<string, string>? ParseEnvLine(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
        {
            return null;
        }

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var key = trimmed[..separatorIndex].Trim().Replace("__", ":", StringComparison.Ordinal);
        var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');

        return new KeyValuePair<string, string>(key, value);
    }
}
