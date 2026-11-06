namespace FluxoCaixaDiario.UI.Models;

public record LancamentoModel(
    Guid Id,
    string Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    string Status,
    DateTime CriadoEm,
    DateTime? CanceladoEm);

public record PaginadoModel<T>(
    IEnumerable<T> Items,
    int Total,
    int Pagina,
    int TamanhoPagina)
{
    public int TotalPaginas => (int)Math.Ceiling((double)Total / TamanhoPagina);
}

public class CriarLancamentoRequest
{
    public string Tipo { get; set; } = "Credito";
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
