using FluxoCaixaDiario.Consolidado.Application.Interfaces;
using FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using RabbitMQ.Client;
using StackExchange.Redis;
using Testcontainers.MsSql;
using Xunit;

namespace FluxoCaixaDiario.Consolidado.IntegrationTests.Infrastructure;

public sealed class ConsolidadoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    private bool _useInMemory;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        try
        {
            _sqlContainer = new MsSqlBuilder()
                .WithPassword(Environment.GetEnvironmentVariable("TEST_SQL_SA_PASSWORD") ?? "ConsolTests@2024!")
                .Build();

            await _sqlContainer.StartAsync();
        }
        catch (ArgumentException ex) when (ex.ParamName == "DockerEndpointAuthConfig")
        {
            _useInMemory = true;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_sqlContainer is not null)
            await _sqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        builder.UseSetting("ApiKey:Key", "test-api-key");

        if (!_useInMemory)
            builder.UseSetting("ConnectionStrings:ConsolidadoDb", _sqlContainer!.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ConsolidadoDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ConsolidadoDbContext>>();

            if (_useInMemory)
            {
                services.AddDbContext<ConsolidadoDbContext>(opts =>
                    opts.UseInMemoryDatabase("ConsolidadoTests"));
            }
            else
            {
                services.AddDbContext<ConsolidadoDbContext>(opts =>
                    opts.UseSqlServer(
                        _sqlContainer!.GetConnectionString(),
                        sql => sql.EnableRetryOnFailure(3)));
            }

            // Substitui IConnection (RabbitMQ.Client raw) por mock
            services.RemoveAll<IConnection>();
            var mockConn = new Mock<IConnection>();
            mockConn.Setup(c => c.IsOpen).Returns(true);
            services.AddSingleton<IConnection>(mockConn.Object);

            // Substitui IConnectionMultiplexer (Redis) por mock
            services.RemoveAll<IConnectionMultiplexer>();
            var mockRedis = new Mock<IConnectionMultiplexer>();
            mockRedis.Setup(r => r.IsConnected).Returns(true);
            services.AddSingleton<IConnectionMultiplexer>(mockRedis.Object);

            // Substitui ICacheService por no-op (sem Redis real)
            services.RemoveAll<ICacheService>();
            services.AddScoped<ICacheService, NullCacheService>();

            // Remove todos os hosted services (MassTransit bus + consumers)
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var s in hostedServices)
                services.Remove(s);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (_useInMemory)
        {
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
            db.Database.EnsureCreated();
        }
        return host;
    }
}

