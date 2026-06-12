using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FluxoCaixaDiario.Consolidado.API.Middlewares;
using FluxoCaixaDiario.Consolidado.Application;
using FluxoCaixaDiario.Consolidado.Infrastructure;
using FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Fluxo de Caixa Diário — Consolidado API",
        Version = "v1",
        Description = "Serviço de consulta do saldo diário consolidado."
    });

    var apiKeyScheme = new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Autenticação por API Key. Informe a chave no header X-Api-Key."
    };
    c.AddSecurityDefinition("ApiKey", apiKeyScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } },
            []
        }
    });
});

builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory
    {
        Uri = new Uri(builder.Configuration["RabbitMQ:ConnectionString"]!)
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services
    .AddHttpContextAccessor()
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromSeconds(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Consolidado.API"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(o =>
            o.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddMeter("FluxoCaixaDiario.Consolidado")
        .AddPrometheusExporter()
        .AddOtlpExporter(o =>
            o.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")));

builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("ConsolidadoDb")!,
        name: "consolidado-db")
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis")
    .AddRabbitMQ(name: "rabbitmq");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseMiddleware<CacheControlMiddleware>();
app.UseRateLimiter();

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.MapControllers().RequireRateLimiting("api");
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();