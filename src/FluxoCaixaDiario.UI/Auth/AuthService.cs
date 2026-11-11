using System.Net.Http.Json;
using FluxoCaixaDiario.UI.Models;

namespace FluxoCaixaDiario.UI.Auth;

public sealed class AuthService(HttpClient http, JwtAuthStateProvider stateProvider)
{
    public async Task<(bool Sucesso, string? Erro)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync("auth/login", request);
            if (!response.IsSuccessStatusCode)
            {
                var problema = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return (false, problema?.GetValueOrDefault("erro") ?? "Credenciais inválidas.");
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse is null)
                return (false, "Resposta inválida do servidor.");

            await stateProvider.MarcarComoAutenticadoAsync(loginResponse.Token);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao conectar: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
        => await stateProvider.MarcarComoDeslogadoAsync();
}
