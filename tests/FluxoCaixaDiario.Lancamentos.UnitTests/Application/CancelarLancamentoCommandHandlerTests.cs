using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.Lancamentos.Application.Commands.CancelarLancamento;
using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.Lancamentos.Application.Metrics;
using FluxoCaixaDiario.Lancamentos.Domain.Entities;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;
using FluxoCaixaDiario.Lancamentos.Domain.Repositories;
using FluxoCaixaDiario.SharedKernel.Events;

namespace FluxoCaixaDiario.Lancamentos.UnitTests.Application;

public sealed class CancelarLancamentoCommandHandlerTests
{
    private readonly Mock<ILancamentoRepository> _repositoryMock = new();
    private readonly Mock<IOutboxRepository> _outboxMock = new();
    private readonly Mock<IAuditContextAccessor> _auditContextMock = new();
    private readonly Mock<ILogger<CancelarLancamentoCommandHandler>> _loggerMock = new();
    private readonly LancamentosMetrics _metrics = new();
    private readonly CancelarLancamentoCommandHandler _handler;

    public CancelarLancamentoCommandHandlerTests()
    {
        _auditContextMock.Setup(a => a.UsuarioOuChave).Returns("test-key");

        _outboxMock
            .Setup(o => o.EnfileirarAsync(It.IsAny<LancamentoCanceladoIntegrationEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CancelarLancamentoCommandHandler(
            _repositoryMock.Object,
            _outboxMock.Object,
            _auditContextMock.Object,
            _metrics,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_LancamentoExistente_DeveRetornarSucesso()
    {
        var lancamento = Lancamento.Criar(
            TipoLancamento.Credito, 100m, "Venda",
            DateOnly.FromDateTime(DateTime.UtcNow)).Value;

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(lancamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamento);
        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(
            new CancelarLancamentoCommand(lancamento.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LancamentoNaoEncontrado_DeveRetornarFalha()
    {
        var id = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lancamento?)null);

        var result = await _handler.Handle(
            new CancelarLancamentoCommand(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("não encontrado");
    }
}