using MediatR;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Consolidado.Application.DTOs;
using FluxoCaixaDiario.Consolidado.Application.Interfaces;
using FluxoCaixaDiario.Consolidado.Domain.Repositories;

namespace FluxoCaixaDiario.Consolidado.Application.Queries.ObterConsolidadoDiario;

public sealed class ObterConsolidadoDiarioQueryHandler(
    IConsolidadoDiarioRepository repository,
    ICacheService cache,
    ILogger<ObterConsolidadoDiarioQueryHandler> logger)
    : IRequestHandler<ObterConsolidadoDiarioQuery, Result<ConsolidadoDiarioDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<Result<ConsolidadoDiarioDto>> Handle(
        ObterConsolidadoDiarioQuery query, CancellationToken ct)
    {
        var cacheKey = $"consolidado:{query.Data:yyyy-MM-dd}";

        var cached = await cache.ObterAsync<ConsolidadoDiarioDto>(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogInformation("Cache hit para {CacheKey}", cacheKey);
            return Result.Success(cached);
        }

        logger.LogInformation("Cache miss para {CacheKey}, consultando banco", cacheKey);
        var consolidado = await repository.ObterPorDataAsync(query.Data, ct);

        if (consolidado is null)
            return Result.Failure<ConsolidadoDiarioDto>(
                $"Não há consolidado para a data {query.Data:yyyy-MM-dd}.");

        var dto = new ConsolidadoDiarioDto(
            consolidado.Data,
            consolidado.TotalCreditos,
            consolidado.TotalDebitos,
            consolidado.SaldoFinal,
            consolidado.QuantidadeLancamentos,
            consolidado.UltimaAtualizacao);

        await cache.DefinirAsync(cacheKey, dto, CacheTtl, ct);

        return Result.Success(dto);
    }
}