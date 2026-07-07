using Core.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using ProjectsService.Core.AuthService;
using ProjectsService.Core.Authorization;
using Shared.Authorization;
using System.Net;

namespace ProjectsService.Core;

public static class Registration
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHandlers(typeof(Registration).Assembly);
        services.AddValidatorsFromAssembly(typeof(Registration).Assembly);
        services.AddPermissionAuthorization<ProjectsPermissions>(JwtBearerDefaults.AuthenticationScheme);
        services.AddHttpContextAccessor();
        services.AddSingleton<IServiceTokenProvider, CachedJwtServiceTokenProvider>();
        services.AddTransient<AuthServiceAuthenticationHandler>();
        services.AddHostedService<AuthDirectorySyncBackgroundService>();

        services
            .AddOptions<AuthServiceClientOptions>()
            .Bind(configuration.GetSection(AuthServiceClientOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.BaseAddress, UriKind.Absolute, out _), "AuthService:BaseAddress must be an absolute URI.")
            .ValidateOnStart();

        services
            .AddHttpClient<IAuthServiceClient, AuthServiceClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthServiceClientOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseAddress);
            })
            .AddHttpMessageHandler<AuthServiceAuthenticationHandler>()
            .AddPolicyHandler((_, request) =>
                request.Method == HttpMethod.Get
                    ? HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
                        .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(150 * Math.Pow(2, retry)))
                    : Policy.NoOpAsync<HttpResponseMessage>())
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        return services;
    }
}
