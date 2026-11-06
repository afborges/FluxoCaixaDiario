using FluxoCaixaDiario.Consolidado.Application.Interfaces;
using FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
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

    public string? SkipReason { get; private set; }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        try
        {
            _sqlContainer = new MsSqlBuilder()
                .WithPassword("ConsolTests@2024!")
                .Build();

            await _sqlContainer.StartAsync();
        }
        catch (ArgumentException ex) when (ex.ParamName == "DockerEndpointAuthConfig")
        {
            SkipReason = "Docker não está disponível. Inicie o Docker Desktop para executar os testes de integração.";
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

        builder.UseSetting("ConnectionStrings:ConsolidadoDb", _sqlContainer!.GetConnectionString());
        builder.UseSetting("ApiKey:Key", "test-api-key");

        builder.ConfigureTestServices(services =>
        {
            // Substitui o DbContext para apontar para o container de teste
            services.RemoveAll<DbContextOptions<ConsolidadoDbContext>>();
            services.AddDbContext<ConsolidadoDbContext>(opts =>
                opts.UseSqlServer(
                    _sqlContainer!.GetConnectionString(),
                    sql => sql.EnableRetryOnFailure(3)));

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
}
