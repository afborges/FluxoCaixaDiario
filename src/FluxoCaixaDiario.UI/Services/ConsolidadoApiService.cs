using System.Net.Http.Json;
using FluxoCaixaDiario.UI.Models;

namespace FluxoCaixaDiario.UI.Services;

public class ConsolidadoApiService(HttpClient httpClient)
{
    public async Task<(List<ConsolidadoModel>? Data, string? Error)> ObterHistoricoAsync(
        DateOnly dataInicio,
        DateOnly dataFim,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"api/consolidado?dataInicio={dataInicio:yyyy-MM-dd}&dataFim={dataFim:yyyy-MM-dd}";
            var response = await httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<ConsolidadoModel>>(cancellationToken: ct);
                return (data, null);
            }
            var erro = await response.Content.ReadAsStringAsync(ct);
            return (null, erro);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
