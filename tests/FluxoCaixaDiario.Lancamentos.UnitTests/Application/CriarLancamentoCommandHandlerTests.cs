using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.Lancamentos.Application.Commands.CriarLancamento;
using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.Lancamentos.Application.Metrics;
using FluxoCaixaDiario.Lancamentos.Domain.Entities;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;
using FluxoCaixaDiario.Lancamentos.Domain.Repositories;
using FluxoCaixaDiario.SharedKernel.Events;

namespace FluxoCaixaDiario.Lancamentos.UnitTests.Application;

public sealed class CriarLancamentoCommandHandlerTests
{
    private readonly Mock<ILancamentoRepository> _repositoryMock = new();
    private readonly Mock<IOutboxRepository> _outboxMock = new();
    private readonly Mock<IAuditContextAccessor> _auditContextMock = new();
    private readonly Mock<ILogger<CriarLancamentoCommandHandler>> _loggerMock = new();
    private readonly LancamentosMetrics _metrics = new();
    private readonly CriarLancamentoCommandHandler _handler;

    public CriarLancamentoCommandHandlerTests()
    {
        _auditContextMock.Setup(a => a.UsuarioOuChave).Returns("test-key");

        _outboxMock
            .Setup(o => o.EnfileirarAsync(It.IsAny<LancamentoCriadoIntegrationEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CriarLancamentoCommandHandler(
            _repositoryMock.Object,
            _outboxMock.Object,
            _auditContextMock.Object,
            _metrics,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DeveRetornarLancamentoCriado()
    {
        var command = new CriarLancamentoCommand(
            "Credito", 500.00m, "Venda de produto", DateOnly.FromDateTime(DateTime.UtcNow));

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tipo.Should().Be("Credito");
        result.Value.Valor.Should().Be(500.00m);
        result.Value.Status.Should().Be("Confirmado");
    }

    [Fact]
    public async Task Handle_ComTipoInvalido_DeveRetornarFalha()
    {
        var command = new CriarLancamentoCommand(
            "TipoInexistente", 100m, "Desc", DateOnly.FromDateTime(DateTime.UtcNow));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Tipo de lançamento inválido");
    }

    [Fact]
    public async Task Handle_ComValorZero_DeveRetornarFalha()
    {
        var command = new CriarLancamentoCommand(
            "Debito", 0m, "Pagamento", DateOnly.FromDateTime(DateTime.UtcNow));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DeveEnfileirarEventoNoOutbox()
    {
        var command = new CriarLancamentoCommand(
            "Debito", 250m, "Compra fornecedor", DateOnly.FromDateTime(DateTime.UtcNow));

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _handler.Handle(command, CancellationToken.None);

        _outboxMock.Verify(
            o => o.EnfileirarAsync(It.IsAny<LancamentoCriadoIntegrationEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DevePersistirNoRepositorio()
    {
        var command = new CriarLancamentoCommand(
            "Credito", 100m, "Receita", DateOnly.FromDateTime(DateTime.UtcNow));

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _handler.Handle(command, CancellationToken.None);

        _repositoryMock.Verify(
            r => r.AdicionarAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(
            r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ComIdempotencyKeyJaExistente_DeveRetornarLancamentoExistente()
    {
        var key = Guid.NewGuid();
        var lancamentoExistente = Lancamento.Criar(
            TipoLancamento.Credito, 300m, "Duplicata",
            DateOnly.FromDateTime(DateTime.UtcNow), key).Value;

        _repositoryMock
            .Setup(r => r.ObterPorIdempotencyKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamentoExistente);

        var command = new CriarLancamentoCommand(
            "Credito", 300m, "Duplicata",
            DateOnly.FromDateTime(DateTime.UtcNow), key);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(lancamentoExistente.Id);
        _repositoryMock.Verify(
            r => r.AdicionarAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}