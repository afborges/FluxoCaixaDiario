using FluxoCaixaDiario.SharedKernel.Events;

namespace FluxoCaixaDiario.Lancamentos.Application.Interfaces;

public interface IEventPublisher
{
    Task PublicarAsync<T>(T evento, CancellationToken ct = default) where T : class;
}