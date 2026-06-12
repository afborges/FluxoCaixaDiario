using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FluxoCaixaDiario.Lancamentos.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Iniciando {RequestName}", requestName);

        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation(
                "Concluído {RequestName} em {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "Erro em {RequestName} após {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}