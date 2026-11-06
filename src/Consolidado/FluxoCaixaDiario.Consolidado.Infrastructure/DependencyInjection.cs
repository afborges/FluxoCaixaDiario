using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using FluxoCaixaDiario.Consolidado.Application.Interfaces;
using FluxoCaixaDiario.Consolidado.Domain.Repositories;
using FluxoCaixaDiario.Consolidado.Infrastructure.Caching;
using FluxoCaixaDiario.Consolidado.Infrastructure.Messaging;
using FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;
using FluxoCaixaDiario.Consolidado.Infrastructure.Persistence.Repositories;

namespace FluxoCaixaDiario.Consolidado.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ConsolidadoDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("ConsolidadoDb"),
                sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<IConsolidadoDiarioRepository, ConsolidadoDiarioRepository>();
        services.AddScoped<IEventoProcessadoRepository, EventoProcessadoRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IAuditContextAccessor, HttpAuditContextAccessor>();

        var redisConn = configuration.GetConnectionString("Redis")!;
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConn));
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddMassTransit(cfg =>
        {
            cfg.SetKebabCaseEndpointNameFormatter();

            cfg.AddConsumer<LancamentoCriadoConsumer>();
            cfg.AddConsumer<LancamentoCanceladoConsumer>();

            cfg.UsingRabbitMq((ctx, rmq) =>
            {
                rmq.Host(configuration["RabbitMQ:Host"], configuration["RabbitMQ:VirtualHost"], h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                rmq.UseMessageRetry(r => r.Exponential(3,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(5)));

                rmq.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}