using System.Diagnostics.Metrics;

namespace FluxoCaixaDiario.Lancamentos.Application.Metrics;

public sealed class LancamentosMetrics : IDisposable
{
    private readonly Meter _meter;

    public readonly Counter<long> LancamentosCriados;
    public readonly Counter<long> LancamentosCancelados;

    public LancamentosMetrics()
    {
        _meter = new Meter("FluxoCaixaDiario.Lancamentos", "1.0.0");
        LancamentosCriados = _meter.CreateCounter<long>(
            "lancamentos_criados_total",
            description: "Total de lançamentos criados com sucesso.");
        LancamentosCancelados = _meter.CreateCounter<long>(
            "lancamentos_cancelados_total",
            description: "Total de lançamentos cancelados com sucesso.");
    }

    public void Dispose() => _meter.Dispose();
}