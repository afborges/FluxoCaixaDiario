using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.Lancamentos.Domain.Repositories;
using FluxoCaixaDiario.Lancamentos.Infrastructure.Messaging;
using FluxoCaixaDiario.Lancamentos.Infrastructure.Outbox;
using FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence;
using FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence.Repositories;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<LancamentosDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("LancamentosDb"),
                sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IEventPublisher, LancamentoEventPublisher>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IAuditContextAccessor, HttpAuditContextAccessor>();

        services.AddHostedService<OutboxProcessor>();

        services.AddMassTransit(cfg =>
        {
            cfg.SetKebabCaseEndpointNameFormatter();

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