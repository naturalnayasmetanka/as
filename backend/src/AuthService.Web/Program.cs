using Framework.Endpoints;
using AuthService.Core;
using AuthService.Infrastructure.Postgres;
using Scalar.AspNetCore;

namespace AuthService.Web;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddCore(builder.Configuration)
            .AddInfrastructurePostgres(builder.Configuration);

        builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();


        var app = builder.Build();

        var coreAssembly = typeof(Registration).Assembly;
        var endpointTypes = coreAssembly.GetTypes().Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        foreach (var type in endpointTypes)
        {
            if (Activator.CreateInstance(type) is IEndpoint endpoint)
                endpoint.MapEndpoint(app);
        }

        app.MapHealthChecks("/helth");

        app.MapEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(opt =>
            {
                opt.RouteTemplate = "openapi/{documentName}.json";
            });
            app.MapScalarApiReference(opt =>
            {
                opt.Title = "Scalar Example";
                opt.Theme = ScalarTheme.Mars;
                opt.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.Http11);
            });
        }

        app.UseHttpsRedirection();

        app.Run();
    }
}
