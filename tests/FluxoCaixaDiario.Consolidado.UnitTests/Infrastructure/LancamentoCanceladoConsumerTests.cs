using FluentAssertions;
using MassTransit;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.SharedKernel.Events;
using FluxoCaixaDiario.Consolidado.Application.Interfaces;
using FluxoCaixaDiario.Consolidado.Application.Metrics;
using FluxoCaixaDiario.Consolidado.Domain.Entities;
using FluxoCaixaDiario.Consolidado.Domain.Repositories;
using FluxoCaixaDiario.Consolidado.Infrastructure.Messaging;

namespace FluxoCaixaDiario.Consolidado.UnitTests.Infrastructure;

public sealed class LancamentoCanceladoConsumerTests
{
    private readonly Mock<IConsolidadoDiarioRepository> _repositoryMock = new();
    private readonly Mock<IEventoProcessadoRepository> _eventosProcessadosMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<IAuditRepository> _auditMock = new();
    private readonly Mock<ILogger<LancamentoCanceladoConsumer>> _loggerMock = new();
    private readonly ConsolidadoMetrics _metrics = new();
    private readonly LancamentoCanceladoConsumer _consumer;

    public LancamentoCanceladoConsumerTests()
    {
        _cacheMock
            .Setup(c => c.RemoverAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _auditMock
            .Setup(a => a.RegistrarAsync(It.IsAny<FluxoCaixaDiario.SharedKernel.Audit.AuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _auditMock
            .Setup(a => a.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _consumer = new LancamentoCanceladoConsumer(
            _repositoryMock.Object,
            _eventosProcessadosMock.Object,
            _cacheMock.Object,
            _auditMock.Object,
            _metrics,
            _loggerMock.Object);
    }

    private static Mock<ConsumeContext<LancamentoCanceladoIntegrationEvent>> CriarContexto(
        LancamentoCanceladoIntegrationEvent evento)
    {
        var contextMock = new Mock<ConsumeContext<LancamentoCanceladoIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(evento);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        contextMock.Setup(c => c.CorrelationId).Returns(Guid.NewGuid());
        var headersMock = new Mock<Headers>();
        headersMock.Setup(h => h.TryGetHeader("X-Api-Key", out It.Ref<object?>.IsAny)).Returns(false);
        contextMock.Setup(c => c.Headers).Returns(headersMock.Object);
        return contextMock;
    }

    [Fact]
    public async Task Consume_EventoDuplicado_DeveIgnorarSemReverterConsolidado()
    {
        var lancamentoId = Guid.NewGuid();
        var evento = new LancamentoCanceladoIntegrationEvent(
            lancamentoId, "Credito", 100m, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow);

        _eventosProcessadosMock
            .Setup(e => e.ExisteAsync(lancamentoId, nameof(LancamentoCanceladoIntegrationEvent), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _consumer.Consume(CriarContexto(evento).Object);

        _repositoryMock.Verify(r => r.ObterPorDataAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<ConsolidadoDiario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ConsolidadoNaoEncontrado_DeveRetornarSemPersistir()
    {
        var data = DateOnly.FromDateTime(DateTime.UtcNow);
        var evento = new LancamentoCanceladoIntegrationEvent(
            Guid.NewGuid(), "Credito", 100m, data, DateTime.UtcNow);

        _eventosProcessadosMock
            .Setup(e => e.ExisteAsync(evento.LancamentoId, nameof(LancamentoCanceladoIntegrationEvent), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsolidadoDiario?)null);

        await _consumer.Consume(CriarContexto(evento).Object);

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<ConsolidadoDiario>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ConsolidadoExistente_DeveReverterLancamentoEInvalidarCache()
    {
        var data = DateOnly.FromDateTime(DateTime.UtcNow);
        var consolidado = ConsolidadoDiario.Criar(data);
        consolidado.AplicarLancamento("Credito", 300m);

        var evento = new LancamentoCanceladoIntegrationEvent(
            Guid.NewGuid(), "Credito", 300m, data, DateTime.UtcNow);
        var cacheKey = $"consolidado:{data:yyyy-MM-dd}";

        _eventosProcessadosMock
            .Setup(e => e.ExisteAsync(evento.LancamentoId, nameof(LancamentoCanceladoIntegrationEvent), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
            .ReturnsAsync(consolidado);
        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<ConsolidadoDiario>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _eventosProcessadosMock
            .Setup(e => e.AdicionarAsync(It.IsAny<EventoProcessado>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _consumer.Consume(CriarContexto(evento).Object);

        consolidado.TotalCreditos.Should().Be(0m);
        _repositoryMock.Verify(r => r.AtualizarAsync(consolidado, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoverAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
