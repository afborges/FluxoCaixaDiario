using FluxoCaixaDiario.SharedKernel.Domain;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;
using FluxoCaixaDiario.Lancamentos.Domain.Events;
using FluxoCaixaDiario.Lancamentos.Domain.ValueObjects;

namespace FluxoCaixaDiario.Lancamentos.Domain.Entities;

public sealed class Lancamento : AggregateRoot
{
    public Guid? IdempotencyKey { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public Valor Valor { get; private set; } = null!;
    public string Descricao { get; private set; } = string.Empty;
    public DateOnly Data { get; private set; }
    public StatusLancamento Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? CanceladoEm { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Lancamento() { }

    public static Result<Lancamento> Criar(
        TipoLancamento tipo,
        decimal valorBruto,
        string descricao,
        DateOnly data,
        Guid? idempotencyKey = null,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return Result.Failure<Lancamento>("A descrição é obrigatória.");

        if (descricao.Length > 255)
            return Result.Failure<Lancamento>("A descrição não pode ultrapassar 255 caracteres.");

        var valorResult = Valor.Criar(valorBruto);
        if (valorResult.IsFailure)
            return Result.Failure<Lancamento>(valorResult.Error);

        var lancamento = new Lancamento
        {
            IdempotencyKey = idempotencyKey,
            Tipo = tipo,
            Valor = valorResult.Value,
            Descricao = descricao.Trim(),
            Data = data,
            Status = StatusLancamento.Confirmado,
            CriadoEm = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        lancamento.AddDomainEvent(new LancamentoCriadoEvent(
            lancamento.Id,
            lancamento.Tipo,
            lancamento.Valor.Quantia,
            lancamento.Descricao,
            lancamento.Data,
            lancamento.CriadoEm));

        return Result.Success(lancamento);
    }

    public Result Cancelar(string? updatedBy = null)
    {
        if (Status == StatusLancamento.Cancelado)
            return Result.Failure("O lançamento já foi cancelado.");

        Status = StatusLancamento.Cancelado;
        CanceladoEm = DateTime.UtcNow;
        UpdatedAt = CanceladoEm;
        UpdatedBy = updatedBy;

        AddDomainEvent(new LancamentoCanceladoEvent(
            Id,
            Tipo,
            Valor.Quantia,
            Data,
            CanceladoEm.Value));

        return Result.Success();
    }
}