using FluentAssertions;
using Xunit;
using FluxoCaixaDiario.Lancamentos.Domain.Entities;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;
using FluxoCaixaDiario.Lancamentos.Domain.Events;

namespace FluxoCaixaDiario.Lancamentos.UnitTests.Domain;

public sealed class LancamentoTests
{
    private static readonly DateOnly DataHoje = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Criar_ComDadosValidos_DeveRetornarSucesso()
    {
        var result = Lancamento.Criar(TipoLancamento.Credito, 100.00m, "Venda", DataHoje);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tipo.Should().Be(TipoLancamento.Credito);
        result.Value.Valor.Quantia.Should().Be(100.00m);
        result.Value.Descricao.Should().Be("Venda");
        result.Value.Data.Should().Be(DataHoje);
        result.Value.Status.Should().Be(StatusLancamento.Confirmado);
    }

    [Fact]
    public void Criar_ComValorZero_DeveRetornarFalha()
    {
        var result = Lancamento.Criar(TipoLancamento.Credito, 0m, "Desc", DataHoje);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("maior que zero");
    }

    [Fact]
    public void Criar_ComValorNegativo_DeveRetornarFalha()
    {
        var result = Lancamento.Criar(TipoLancamento.Debito, -50m, "Desc", DataHoje);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("maior que zero");
    }

    [Fact]
    public void Criar_ComDescricaoVazia_DeveRetornarFalha()
    {
        var result = Lancamento.Criar(TipoLancamento.Credito, 100m, "", DataHoje);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("descrição é obrigatória");
    }

    [Fact]
    public void Criar_ComDescricaoMaiorQue255Chars_DeveRetornarFalha()
    {
        var descricaoLonga = new string('x', 256);
        var result = Lancamento.Criar(TipoLancamento.Credito, 100m, descricaoLonga, DataHoje);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("255 caracteres");
    }

    [Fact]
    public void Criar_DeveLançarLancamentoCriadoEvent()
    {
        var result = Lancamento.Criar(TipoLancamento.Credito, 250.50m, "Teste", DataHoje);

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LancamentoCriadoEvent>();

        var evento = (LancamentoCriadoEvent)result.Value.DomainEvents.First();
        evento.LancamentoId.Should().Be(result.Value.Id);
        evento.Tipo.Should().Be(TipoLancamento.Credito);
        evento.Valor.Should().Be(250.50m);
    }

    [Fact]
    public void Cancelar_LancamentoConfirmado_DeveRetornarSucesso()
    {
        var lancamento = Lancamento.Criar(TipoLancamento.Debito, 50m, "Compra", DataHoje).Value;
        lancamento.ClearDomainEvents();

        var result = lancamento.Cancelar();

        result.IsSuccess.Should().BeTrue();
        lancamento.Status.Should().Be(StatusLancamento.Cancelado);
        lancamento.CanceladoEm.Should().NotBeNull();
    }

    [Fact]
    public void Cancelar_LancamentoJaCancelado_DeveRetornarFalha()
    {
        var lancamento = Lancamento.Criar(TipoLancamento.Debito, 50m, "Compra", DataHoje).Value;
        lancamento.Cancelar();

        var result = lancamento.Cancelar();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("já foi cancelado");
    }

    [Fact]
    public void Cancelar_DeveLancarLancamentoCanceladoEvent()
    {
        var lancamento = Lancamento.Criar(TipoLancamento.Credito, 100m, "Venda", DataHoje).Value;
        lancamento.ClearDomainEvents();

        lancamento.Cancelar();

        lancamento.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LancamentoCanceladoEvent>();
    }

    [Theory]
    [InlineData(99.99)]
    [InlineData(0.01)]
    [InlineData(999999.99)]
    public void Criar_ComValoresLimite_DeveRetornarSucesso(decimal valor)
    {
        var result = Lancamento.Criar(TipoLancamento.Credito, valor, "Desc", DataHoje);

        result.IsSuccess.Should().BeTrue();
        result.Value.Valor.Quantia.Should().Be(valor);
    }

    [Fact]
    public void Criar_ComTipoDebito_DeveCriarCorretamente()
    {
        var result = Lancamento.Criar(TipoLancamento.Debito, 75.00m, "Pagamento", DataHoje);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tipo.Should().Be(TipoLancamento.Debito);
    }
}