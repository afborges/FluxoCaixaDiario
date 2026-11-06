using MediatR;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.SharedKernel.Events;
using FluxoCaixaDiario.Lancamentos.Application.DTOs;
using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.Lancamentos.Application.Metrics;
using FluxoCaixaDiario.Lancamentos.Domain.Entities;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;
using FluxoCaixaDiario.Lancamentos.Domain.Events;
using FluxoCaixaDiario.Lancamentos.Domain.Repositories;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands.CriarLancamento;

public sealed class CriarLancamentoCommandHandler(
    ILancamentoRepository repository,
    IOutboxRepository outbox,
    IAuditContextAccessor auditContext,
    LancamentosMetrics metrics,
    ILogger<CriarLancamentoCommandHandler> logger)
    : IRequestHandler<CriarLancamentoCommand, Result<LancamentoDto>>
{
    public async Task<Result<LancamentoDto>> Handle(
        CriarLancamentoCommand command, CancellationToken ct)
    {
        if (command.IdempotencyKey.HasValue)
        {
            var existente = await repository.ObterPorIdempotencyKeyAsync(
                command.IdempotencyKey.Value, ct);
            if (existente is not null)
            {
                logger.LogInformation(
                    "Lançamento já existente para IdempotencyKey {Key}. Retornando existente.",
                    command.IdempotencyKey);
                return Result.Success(ToDto(existente));
            }
        }

        if (!Enum.TryParse<TipoLancamento>(command.Tipo, out var tipo))
            return Result.Failure<LancamentoDto>($"Tipo de lançamento inválido: {command.Tipo}");

        var result = Lancamento.Criar(tipo, command.Valor, command.Descricao, command.Data,
            command.IdempotencyKey, auditContext.UsuarioOuChave);
        if (result.IsFailure)
            return Result.Failure<LancamentoDto>(result.Error);

        var lancamento = result.Value;

        await repository.AdicionarAsync(lancamento, ct);

        foreach (var domainEvent in lancamento.DomainEvents.OfType<LancamentoCriadoEvent>())
        {
            var integrationEvent = new LancamentoCriadoIntegrationEvent(
                domainEvent.LancamentoId,
                domainEvent.Tipo.ToString(),
                domainEvent.Valor,
                domainEvent.Descricao,
                domainEvent.Data,
                domainEvent.OcorridoEm);

            await outbox.EnfileirarAsync(integrationEvent, ct);
        }

        lancamento.ClearDomainEvents();

        await repository.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Lançamento criado: {LancamentoId} | Tipo: {Tipo} | Valor: {Valor} | Data: {Data}",
            lancamento.Id, lancamento.Tipo, lancamento.Valor.Quantia, lancamento.Data);

        metrics.LancamentosCriados.Add(1, new KeyValuePair<string, object?>("tipo", lancamento.Tipo.ToString()));

        return Result.Success(ToDto(lancamento));
    }

    private static LancamentoDto ToDto(Lancamento l) => new(
        l.Id, l.Tipo.ToString(), l.Valor.Quantia, l.Descricao,
        l.Data, l.Status.ToString(), l.CriadoEm, l.CanceladoEm);
}