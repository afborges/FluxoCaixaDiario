using System.Net.Http.Json;
using FluxoCaixaDiario.UI.Models;

namespace FluxoCaixaDiario.UI.Services;

public class LancamentosApiService(HttpClient httpClient)
{
    public async Task<PaginadoModel<LancamentoModel>?> ListarAsync(
        DateOnly? dataInicio = null,
        DateOnly? dataFim = null,
        string? tipo = null,
        string? status = null,
        int pagina = 1,
        int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var url = BuildUrl("api/lancamentos", dataInicio, dataFim, tipo, status, pagina, tamanhoPagina);
        try
        {
            return await httpClient.GetFromJsonAsync<PaginadoModel<LancamentoModel>>(url, ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, LancamentoModel? Data, string? Error)> CriarAsync(
        CriarLancamentoRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/lancamentos", request, ct);
            if (response.IsSuccessStatusCode)
                return (true, await response.Content.ReadFromJsonAsync<LancamentoModel>(cancellationToken: ct), null);

            var erro = await response.Content.ReadAsStringAsync(ct);
            return (false, null, erro);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> CancelarAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"api/lancamentos/{id}", ct);
            if (response.IsSuccessStatusCode) return (true, null);
            var erro = await response.Content.ReadAsStringAsync(ct);
            return (false, erro);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string BuildUrl(
        string baseUrl,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        string? tipo,
        string? status,
        int pagina,
        int tamanhoPagina)
    {
        var parts = new List<string>();
        if (dataInicio.HasValue) parts.Add($"dataInicio={dataInicio.Value:yyyy-MM-dd}");
        if (dataFim.HasValue) parts.Add($"dataFim={dataFim.Value:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(tipo)) parts.Add($"tipo={Uri.EscapeDataString(tipo)}");
        if (!string.IsNullOrEmpty(status)) parts.Add($"status={Uri.EscapeDataString(status)}");
        parts.Add($"pagina={pagina}");
        parts.Add($"tamanhoPagina={tamanhoPagina}");
        return parts.Count > 0 ? $"{baseUrl}?{string.Join("&", parts)}" : baseUrl;
    }
}
