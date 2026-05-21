using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Serilog;
using Sincera.Policies.Api.Endpoints;
using Sincera.Policies.Api.Middleware;
using Sincera.Policies.Application;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Sincera Policies API", Version = "v1" });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger(c =>
{
    c.RouteTemplate = "openapi/{documentName}.json";
});

app.MapScalarApiReference(options =>
{
    options.Title = "Sincera Policies API";
    options.OpenApiRoutePattern = "/openapi/{documentName}.json";
});
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

app.MapPolicyEndpoints();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
}

app.Run();

public partial class Program { }
