using FluentAssertions;
using Xunit;
using FluxoCaixaDiario.Consolidado.Domain.Entities;

namespace FluxoCaixaDiario.Consolidado.UnitTests.Domain;

public sealed class ConsolidadoDiarioTests
{
    private static readonly DateOnly DataTeste = new(2024, 1, 15);

    [Fact]
    public void Criar_DeveInicializarComValoresZerados()
    {
        var consolidado = ConsolidadoDiario.Criar(DataTeste);

        consolidado.Data.Should().Be(DataTeste);
        consolidado.TotalCreditos.Should().Be(0);
        consolidado.TotalDebitos.Should().Be(0);
        consolidado.SaldoFinal.Should().Be(0);
        consolidado.QuantidadeLancamentos.Should().Be(0);
    }

    [Fact]
    public void AplicarLancamento_Credito_DeveIncrementarTotalCreditos()
    {
        var consolidado = ConsolidadoDiario.Criar(DataTeste);

        consolidado.AplicarLancamento("Credito", 500m);

        consolidado.TotalCreditos.Should().Be(500m);
        consolidado.TotalDebitos.Should().Be(0m);
        consolidado.SaldoFinal.Should().Be(500m);
        consolidado.QuantidadeLancamentos.Should().Be(1);
    }

    [Fact]
    public void AplicarLancamento_Debito_DeveIncrementarTotalDebitos()
    {
        var consolidado = ConsolidadoDiario.Criar(DataTeste);

        consolidado.AplicarLancamento("Debito", 200m);

        consolidado.TotalDebitos.Should().Be(200m);
        consolidado.TotalCreditos.Should().Be(0m);
        consolidado.SaldoFinal.Should().Be(-200m);
        consolidado.QuantidadeLancamentos.Should().Be(1);
    }

    [Fact]
    public void AplicarLancamento_MultiplosDias_DeveSomarCorretamente()
    {
        var consolidado = ConsolidadoDiario.Criar(DataTeste);

        consolidado.AplicarLancamento("Credito", 1000m);
        consolidado.AplicarLancamento("Credito", 500m);
        consolidado.AplicarLancamento("Debito", 300m);

        consolidado.TotalCreditos.Should().Be(1500m);
        consolidado.TotalDebitos.Should().Be(300m);
        consolidado.SaldoFinal.Should().Be(1200m);
        consolidado.QuantidadeLancamentos.Should().Be(3);
    }

    [Fact]
    public void ReverterLancamento_Credito_DeveSubtrairDeTotalCreditos()
    {
        var consolidado = ConsolidadoDiario.Criar(DataTeste);
        consolidado.AplicarLancamento("Credito", 500m);
        consolidado.AplicarLancamento("Credito", 300m);

        consolidado.ReverterLancamento("Credito", 300m);

        consolidado.TotalCreditos.Should().Be(500m);
        consolidado.QuantidadeLancamentos.Should().Be(1);
    }

    [Fact]
    public void ReverterLancamento_DebitoAlemDoDisponivel_DeveManterZero()
    {
        var consolidado = ConsolidadoDiario.Criar(DataTeste);
        consolidado.AplicarLancamento("Debito", 100m);

        consolidado.ReverterLancamento("Debito", 500m);

        consolidado.TotalDebitos.Should().Be(0m);
    }

    [Fact]
    public void SaldoFinal_DeveSerCalculadoCorreto()
    {
        var consolidado = ConsolidadoDiario.Criar(DataTeste);

        consolidado.AplicarLancamento("Credito", 1000m);
        consolidado.AplicarLancamento("Debito", 400m);

        consolidado.SaldoFinal.Should().Be(600m);
    }

    [Fact]
    public void UltimaAtualizacao_DeveSerAtualizadaAoCadaOperacao()
    {
        var consolidado = ConsolidadoDiario.Criar(DataTeste);
        var antes = consolidado.UltimaAtualizacao;

        consolidado.AplicarLancamento("Credito", 100m);

        consolidado.UltimaAtualizacao.Should().BeOnOrAfter(antes);
    }
}