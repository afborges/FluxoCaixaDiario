using FluentAssertions;
using Xunit;
using FluxoCaixaDiario.SharedKernel.Outbox;

namespace FluxoCaixaDiario.Lancamentos.UnitTests.Domain;

public sealed class OutboxMessageTests
{
    [Fact]
    public void Criar_DeveInicializarComTentativasZeroEProcessadoEmNulo()
    {
        var mensagem = OutboxMessage.Criar("EventType", "{\"id\":1}");

        mensagem.TentativasRetry.Should().Be(0);
        mensagem.ProcessadoEm.Should().BeNull();
        mensagem.EventType.Should().Be("EventType");
        mensagem.Payload.Should().Be("{\"id\":1}");
    }

    [Fact]
    public void IncrementarTentativa_DeveAumentarContadorACadaChamada()
    {
        var mensagem = OutboxMessage.Criar("EventType", "{}");

        mensagem.IncrementarTentativa();
        mensagem.IncrementarTentativa();
        mensagem.IncrementarTentativa();

        mensagem.TentativasRetry.Should().Be(3);
    }

    [Fact]
    public void MarcarComoProcessado_DeveDefinirProcessadoEmComDataAtual()
    {
        var mensagem = OutboxMessage.Criar("EventType", "{}");
        var antes = DateTime.UtcNow.AddSeconds(-1);

        mensagem.MarcarComoProcessado();

        mensagem.ProcessadoEm.Should().NotBeNull();
        mensagem.ProcessadoEm.Should().BeAfter(antes);
    }

    [Fact]
    public void OutboxProcessor_FiltroMaxTentativas_DevePularMensagemComCincoTentativas()
    {
        // Simula o filtro aplicado pelo OutboxProcessor:
        // Where(m => m.ProcessadoEm == null && m.TentativasRetry < MaxTentativas)
        const int MaxTentativas = 5;
        var mensagem = OutboxMessage.Criar("EventType", "{}");

        for (int i = 0; i < MaxTentativas; i++)
            mensagem.IncrementarTentativa();

        var deveProcessar = mensagem.ProcessadoEm == null && mensagem.TentativasRetry < MaxTentativas;

        deveProcessar.Should().BeFalse("mensagem com 5 tentativas deve ser ignorada pelo OutboxProcessor");
    }

    [Fact]
    public void OutboxProcessor_FiltroMaxTentativas_DeveIncluirMensagemComQuatroTentativas()
    {
        const int MaxTentativas = 5;
        var mensagem = OutboxMessage.Criar("EventType", "{}");

        for (int i = 0; i < MaxTentativas - 1; i++)
            mensagem.IncrementarTentativa();

        var deveProcessar = mensagem.ProcessadoEm == null && mensagem.TentativasRetry < MaxTentativas;

        deveProcessar.Should().BeTrue("mensagem com 4 tentativas ainda deve ser reprocessada");
    }

    [Fact]
    public void OutboxProcessor_FiltroProcessadoEm_DeveExcluirMensagemJaProcessada()
    {
        const int MaxTentativas = 5;
        var mensagem = OutboxMessage.Criar("EventType", "{}");
        mensagem.MarcarComoProcessado();

        var deveProcessar = mensagem.ProcessadoEm == null && mensagem.TentativasRetry < MaxTentativas;

        deveProcessar.Should().BeFalse("mensagem já processada deve ser ignorada pelo OutboxProcessor");
    }
}
