using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using FluxoCaixaDiario.Lancamentos.Application.Behaviors;
using FluxoCaixaDiario.Lancamentos.Application.Metrics;

namespace FluxoCaixaDiario.Lancamentos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddSingleton<LancamentosMetrics>();

        return services;
    }
}