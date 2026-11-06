namespace FluxoCaixaDiario.Lancamentos.Application.DTOs;

public sealed record PaginadoDto<T>(
    IEnumerable<T> Items,
    int Total,
    int Pagina,
    int TamanhoPagina)
{
    public int TotalPaginas => (int)Math.Ceiling((double)Total / TamanhoPagina);
}