using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence;
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
using Testcontainers.MsSql;
using Xunit;

namespace FluxoCaixaDiario.Lancamentos.IntegrationTests.Infrastructure;

public sealed class LancamentosApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    private bool _useInMemory;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        try
        {
            _sqlContainer = new MsSqlBuilder()
                .WithPassword(Environment.GetEnvironmentVariable("TEST_SQL_SA_PASSWORD") ?? "LancTests@2024!")
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
            builder.UseSetting("ConnectionStrings:LancamentosDb", _sqlContainer!.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<LancamentosDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<LancamentosDbContext>>();

            if (_useInMemory)
            {
                services.AddDbContext<LancamentosDbContext>(opts =>
                    opts.UseInMemoryDatabase("LancamentosTests"));
            }
            else
            {
                services.AddDbContext<LancamentosDbContext>(opts =>
                    opts.UseSqlServer(
                        _sqlContainer!.GetConnectionString(),
                        sql => sql.EnableRetryOnFailure(3)));
            }

            // Substitui IConnection (RabbitMQ.Client raw) por mock
            services.RemoveAll<IConnection>();
            var mockConn = new Mock<IConnection>();
            mockConn.Setup(c => c.IsOpen).Returns(true);
            services.AddSingleton<IConnection>(mockConn.Object);

            // Substitui IEventPublisher por no-op (sem RabbitMQ)
            services.RemoveAll<IEventPublisher>();
            services.AddScoped<IEventPublisher, NoOpEventPublisher>();

            // Remove todos os hosted services (OutboxProcessor + MassTransit bus)
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
            var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
            db.Database.EnsureCreated();
        }
        return host;
    }
}

