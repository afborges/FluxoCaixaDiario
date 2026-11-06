using FluentAssertions;
using Xunit;
using FluxoCaixaDiario.Lancamentos.Domain.ValueObjects;

namespace FluxoCaixaDiario.Lancamentos.UnitTests.Domain;

public sealed class ValorTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(1000000.00)]
    [InlineData(99.99)]
    public void Criar_ComValorPositivo_DeveRetornarSucesso(decimal quantia)
    {
        var result = Valor.Criar(quantia);

        result.IsSuccess.Should().BeTrue();
        result.Value.Quantia.Should().Be(quantia);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Criar_ComValorNaoPositivo_DeveRetornarFalha(decimal quantia)
    {
        var result = Valor.Criar(quantia);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("maior que zero");
    }

    [Theory]
    [InlineData(1.001)]
    [InlineData(99.999)]
    public void Criar_ComMaisDeDuasCasasDecimais_DeveRetornarFalha(decimal quantia)
    {
        var result = Valor.Criar(quantia);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("2 casas decimais");
    }

    [Fact]
    public void Igualdade_ValoresIguais_DevemSerIguais()
    {
        var a = Valor.Criar(100.00m).Value;
        var b = Valor.Criar(100.00m).Value;

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Igualdade_ValoresDiferentes_NaoDevemSerIguais()
    {
        var a = Valor.Criar(100.00m).Value;
        var b = Valor.Criar(200.00m).Value;

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }
}