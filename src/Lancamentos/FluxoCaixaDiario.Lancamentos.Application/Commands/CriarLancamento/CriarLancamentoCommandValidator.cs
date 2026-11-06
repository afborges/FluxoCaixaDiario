using FluentValidation;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands.CriarLancamento;

public sealed class CriarLancamentoCommandValidator : AbstractValidator<CriarLancamentoCommand>
{
    private static readonly string[] TiposValidos = ["Credito", "Debito"];

    public CriarLancamentoCommandValidator()
    {
        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("O tipo é obrigatório.")
            .Must(t => TiposValidos.Contains(t))
            .WithMessage("O tipo deve ser 'Credito' ou 'Debito'.");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(255).WithMessage("A descrição não pode ultrapassar 255 caracteres.");

        RuleFor(x => x.Data)
            .NotEmpty().WithMessage("A data é obrigatória.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("A data não pode ser futura.");
    }
}