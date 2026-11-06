using FluxoCaixaDiario.SharedKernel.Domain;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;

namespace FluxoCaixaDiario.Lancamentos.Domain.ValueObjects;

public sealed class Valor : ValueObject
{
    public decimal Quantia { get; }

    private Valor(decimal quantia) => Quantia = quantia;

    public static SharedKernel.Result.Result<Valor> Criar(decimal quantia)
    {
        if (quantia <= 0)
            return SharedKernel.Result.Result.Failure<Valor>("O valor deve ser maior que zero.");
        if (Math.Round(quantia, 2) != quantia)
            return SharedKernel.Result.Result.Failure<Valor>("O valor não pode ter mais de 2 casas decimais.");
        return SharedKernel.Result.Result.Success(new Valor(quantia));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Quantia;
    }

    public override string ToString() => Quantia.ToString("F2");
}