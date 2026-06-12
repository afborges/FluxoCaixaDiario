using MediatR;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.SharedKernel.Events;
using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.Lancamentos.Application.Metrics;
using FluxoCaixaDiario.Lancamentos.Domain.Events;
using FluxoCaixaDiario.Lancamentos.Domain.Repositories;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands.CancelarLancamento;

public sealed class CancelarLancamentoCommandHandler(
    ILancamentoRepository repository,
    IOutboxRepository outbox,
    IAuditContextAccessor auditContext,
    LancamentosMetrics metrics,
    ILogger<CancelarLancamentoCommandHandler> logger)
    : IRequestHandler<CancelarLancamentoCommand, Result>
{
    public async Task<Result> Handle(CancelarLancamentoCommand command, CancellationToken ct)
    {
        var lancamento = await repository.ObterPorIdAsync(command.Id, ct);
        if (lancamento is null)
            return Result.Failure($"Lançamento {command.Id} não encontrado.");

        var cancelResult = lancamento.Cancelar(auditContext.UsuarioOuChave);
        if (cancelResult.IsFailure)
            return cancelResult;

        await repository.AtualizarAsync(lancamento, ct);

        foreach (var domainEvent in lancamento.DomainEvents.OfType<LancamentoCanceladoEvent>())
        {
            var integrationEvent = new LancamentoCanceladoIntegrationEvent(
                domainEvent.LancamentoId,
                domainEvent.Tipo.ToString(),
                domainEvent.Valor,
                domainEvent.Data,
                domainEvent.OcorridoEm);

            await outbox.EnfileirarAsync(integrationEvent, ct);
        }

        lancamento.ClearDomainEvents();

        await repository.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Lançamento cancelado: {LancamentoId}", command.Id);

        metrics.LancamentosCancelados.Add(1);

        return Result.Success();
    }
}