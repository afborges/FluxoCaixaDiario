using MediatR;
using Microsoft.Extensions.DependencyInjection;
using FluxoCaixaDiario.Consolidado.Application.Metrics;

namespace FluxoCaixaDiario.Consolidado.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddSingleton<ConsolidadoMetrics>();

        return services;
    }
}