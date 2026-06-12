using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.SharedKernel.Audit;
using FluxoCaixaDiario.SharedKernel.Result;

namespace FluxoCaixaDiario.Lancamentos.Application.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditRepository auditRepository,
    IAuditContextAccessor auditContext,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestType = typeof(TRequest);
        var isCommand = requestType.Namespace?.Contains(".Commands.") == true;

        if (!isCommand)
            return await next();

        var operacao = requestType.Name;
        var correlationId = auditContext.CorrelationId;
        var usuarioOuChave = auditContext.UsuarioOuChave;
        string? dadosEntrada = null;
        try
        {
            dadosEntrada = JsonSerializer.Serialize(request, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AuditBehavior: falha ao serializar entrada de {Operacao}", operacao);
        }

        var sw = Stopwatch.StartNew();
        Exception? excecao = null;
        TResponse response;

        try
        {
            response = await next();
        }
        catch (Exception ex)
        {
            excecao = ex;
            sw.Stop();

            await RegistrarAuditAsync(operacao, false, sw.ElapsedMilliseconds,
                usuarioOuChave, correlationId, dadosEntrada, null,
                ex.Message, ct);

            throw;
        }

        sw.Stop();

        var sucesso = true;
        string? mensagemErro = null;
        string? dadosSaida = null;

        if (response is Result resultado)
        {
            sucesso = resultado.IsSuccess;
            mensagemErro = resultado.IsFailure ? resultado.Error : null;
        }

        try
        {
            dadosSaida = JsonSerializer.Serialize(response, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AuditBehavior: falha ao serializar saída de {Operacao}", operacao);
        }

        await RegistrarAuditAsync(operacao, sucesso, sw.ElapsedMilliseconds,
            usuarioOuChave, correlationId, dadosEntrada, dadosSaida,
            mensagemErro, ct);

        return response;
    }

    private async Task RegistrarAuditAsync(
        string operacao,
        bool sucesso,
        long duracaoMs,
        string? usuarioOuChave,
        string? correlationId,
        string? dadosEntrada,
        string? dadosSaida,
        string? mensagemErro,
        CancellationToken ct)
    {
        try
        {
            var auditLog = AuditLog.Criar(
                servico: "Lancamentos",
                operacao: operacao,
                sucesso: sucesso,
                duracaoMs: duracaoMs,
                usuarioOuChave: usuarioOuChave,
                correlationId: correlationId,
                dadosEntrada: dadosEntrada,
                dadosSaida: dadosSaida,
                mensagemErro: mensagemErro);

            await auditRepository.RegistrarAsync(auditLog, ct);
            await auditRepository.SalvarAlteracoesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuditBehavior: falha ao persistir audit log de {Operacao}", operacao);
        }
    }
}