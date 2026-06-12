using System.Diagnostics.Metrics;

namespace FluxoCaixaDiario.Consolidado.Application.Metrics;

public sealed class ConsolidadoMetrics : IDisposable
{
    private readonly Meter _meter;

    public readonly Counter<long> CacheHits;
    public readonly Counter<long> CacheMisses;
    public readonly Counter<long> ConsolidacoesProcessadas;

    public ConsolidadoMetrics()
    {
        _meter = new Meter("FluxoCaixaDiario.Consolidado", "1.0.0");
        CacheHits = _meter.CreateCounter<long>(
            "consolidado_cache_hits_total",
            description: "Total de leituras atendidas pelo cache Redis.");
        CacheMisses = _meter.CreateCounter<long>(
            "consolidado_cache_misses_total",
            description: "Total de leituras não encontradas no cache Redis.");
        ConsolidacoesProcessadas = _meter.CreateCounter<long>(
            "consolidacoes_processadas_total",
            description: "Total de eventos de lançamento processados pelo Consolidado.");
    }

    public void Dispose() => _meter.Dispose();
}