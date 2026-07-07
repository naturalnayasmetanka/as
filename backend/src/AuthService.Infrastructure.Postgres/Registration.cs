using AuthService.Core.Authentication;
using AuthService.Core.Authorization;
using AuthService.Core.Database;
using AuthService.Core.Database.Abstractions;
using AuthService.Domain.Accounts;
using AuthService.Domain.Roles;
using AuthService.Infrastructure.Postgres.Authorization;
using AuthService.Infrastructure.Postgres.Repositories;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Shared.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.AddScoped<IAdminUserReadRepository, AdminUserReadRepository>();
        services.AddScoped<SystemRoleSeeder>();

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

    private static IServiceCollection AddIdentity(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
            .Validate(o => o.AccessTokenExpireMinutes is > 0 and <= 30, "Jwt:AccessTokenExpireMinutes must be between 1 and 30.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), "Jwt:SigningKey is required.")
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= 32, "Jwt:SigningKey must be at least 32 bytes.")
            .ValidateOnStart();

        services.AddScoped<IJwtTokenService, JwtTokenService>();

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
            options.SignIn.RequireConfirmedEmail = false;

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

        services.AddAuthentication()
             .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
             {
                 var jwtOptions = configuration
                     .GetSection(JwtOptions.SectionName)
                     .Get<JwtOptions>()
                     ?? throw new InvalidOperationException("Jwt options are not configured.");

                 options.MapInboundClaims = false;

                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuer = true,
                     ValidIssuer = jwtOptions.Issuer,

                     ValidateAudience = true,
                     ValidAudience = jwtOptions.Audience,

                     ValidateLifetime = true,
                     ClockSkew = TimeSpan.Zero,

                     ValidateIssuerSigningKey = true,
                     IssuerSigningKey = new SymmetricSecurityKey(
                         Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

                     NameClaimType = "name",
                     RoleClaimType = "role"
                 };
             });

        services.AddAuthorization();

        return services;
    }
}
