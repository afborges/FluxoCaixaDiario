using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.Consolidado.Application.DTOs;
using FluxoCaixaDiario.Consolidado.Application.Interfaces;
using FluxoCaixaDiario.Consolidado.Application.Queries.ObterConsolidadoDiario;
using FluxoCaixaDiario.Consolidado.Domain.Entities;
using FluxoCaixaDiario.Consolidado.Domain.Repositories;

namespace FluxoCaixaDiario.Consolidado.UnitTests.Application;

public sealed class ObterConsolidadoDiarioQueryHandlerTests
{
    private static readonly DateOnly DataTeste = new(2024, 1, 15);

    private readonly Mock<IConsolidadoDiarioRepository> _repositoryMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ILogger<ObterConsolidadoDiarioQueryHandler>> _loggerMock = new();
    private readonly ObterConsolidadoDiarioQueryHandler _handler;

    public ObterConsolidadoDiarioQueryHandlerTests()
    {
        _handler = new ObterConsolidadoDiarioQueryHandler(
            _repositoryMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_CacheHit_DeveRetornarDoCache()
    {
        var dtoCache = new ConsolidadoDiarioDto(
            DataTeste, 1000m, 400m, 600m, 3, DateTime.UtcNow);

        _cacheMock
            .Setup(c => c.ObterAsync<ConsolidadoDiarioDto>(
                $"consolidado:{DataTeste:yyyy-MM-dd}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtoCache);

        var result = await _handler.Handle(
            new ObterConsolidadoDiarioQuery(DataTeste), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dtoCache);

        _repositoryMock.Verify(
            r => r.ObterPorDataAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_DeveConsultarBancoEPopularCache()
    {
        _cacheMock
            .Setup(c => c.ObterAsync<ConsolidadoDiarioDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsolidadoDiarioDto?)null);

        var consolidado = ConsolidadoDiario.Criar(DataTeste);
        consolidado.AplicarLancamento("Credito", 1000m);
        consolidado.AplicarLancamento("Debito", 400m);

        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(DataTeste, It.IsAny<CancellationToken>()))
            .ReturnsAsync(consolidado);

        _cacheMock
            .Setup(c => c.DefinirAsync(
                It.IsAny<string>(),
                It.IsAny<ConsolidadoDiarioDto>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new ObterConsolidadoDiarioQuery(DataTeste), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().Be(DataTeste);
        result.Value.TotalCreditos.Should().Be(1000m);
        result.Value.TotalDebitos.Should().Be(400m);
        result.Value.SaldoFinal.Should().Be(600m);

        _cacheMock.Verify(
            c => c.DefinirAsync(
                $"consolidado:{DataTeste:yyyy-MM-dd}",
                It.IsAny<ConsolidadoDiarioDto>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SemConsolidadoNoBanco_DeveRetornarFalha()
    {
        _cacheMock
            .Setup(c => c.ObterAsync<ConsolidadoDiarioDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsolidadoDiarioDto?)null);

        _repositoryMock
            .Setup(r => r.ObterPorDataAsync(DataTeste, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsolidadoDiario?)null);

        var result = await _handler.Handle(
            new ObterConsolidadoDiarioQuery(DataTeste), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("2024-01-15");
    }
}