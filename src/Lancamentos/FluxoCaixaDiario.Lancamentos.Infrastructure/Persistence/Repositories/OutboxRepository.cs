using System.Text.Json;
using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.SharedKernel.Outbox;
using FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository(LancamentosDbContext context) : IOutboxRepository
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task EnfileirarAsync<T>(T evento, CancellationToken ct = default) where T : class
    {
        var eventType = typeof(T).FullName!;
        var payload = JsonSerializer.Serialize(evento, _jsonOptions);
        var mensagem = OutboxMessage.Criar(eventType, payload);
        await context.OutboxMessages.AddAsync(mensagem, ct);
    }
}