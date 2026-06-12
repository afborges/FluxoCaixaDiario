using FluxoCaixaDiario.SharedKernel.Domain;

namespace FluxoCaixaDiario.Consolidado.Domain.Entities;

public sealed class ConsolidadoDiario : AggregateRoot
{
    public DateOnly Data { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public decimal SaldoFinal => TotalCreditos - TotalDebitos;
    public int QuantidadeLancamentos { get; private set; }
    public DateTime UltimaAtualizacao { get; private set; }

    private ConsolidadoDiario() { }

    public static ConsolidadoDiario Criar(DateOnly data) => new()
    {
        Data = data,
        TotalCreditos = 0,
        TotalDebitos = 0,
        QuantidadeLancamentos = 0,
        UltimaAtualizacao = DateTime.UtcNow
    };

    public void AplicarLancamento(string tipo, decimal valor)
    {
        if (tipo == "Credito")
            TotalCreditos += valor;
        else
            TotalDebitos += valor;

        QuantidadeLancamentos++;
        UltimaAtualizacao = DateTime.UtcNow;
    }

    public void ReverterLancamento(string tipo, decimal valor)
    {
        if (tipo == "Credito")
            TotalCreditos = Math.Max(0, TotalCreditos - valor);
        else
            TotalDebitos = Math.Max(0, TotalDebitos - valor);

        QuantidadeLancamentos = Math.Max(0, QuantidadeLancamentos - 1);
        UltimaAtualizacao = DateTime.UtcNow;
    }
}