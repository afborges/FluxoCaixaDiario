using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace FluxoCaixaDiario.UI.Auth;

public sealed class JwtAuthStateProvider(IJSRuntime js) : AuthenticationStateProvider
{
    private const string TokenKey = "auth_token";
    private static readonly AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await ObterTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return _anonymous;

        var claims = ExtrairClaims(token);
        if (claims is null)
            return _anonymous;

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task MarcarComoAutenticadoAsync(string token)
    {
        await js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        var claims = ExtrairClaims(token) ?? [];
        var identity = new ClaimsIdentity(claims, "jwt");
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
    }

    public async Task MarcarComoDeslogadoAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }

    public async Task<string?> ObterTokenAsync()
        => await js.InvokeAsync<string?>("localStorage.getItem", TokenKey);

    private static IEnumerable<Claim>? ExtrairClaims(string token)
    {
        try
        {
            var payload = token.Split('.')[1];
            var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict is null) return null;

            var claims = new List<Claim>();
            foreach (var (key, value) in dict)
            {
                var claimType = key switch
                {
                    "sub" => ClaimTypes.NameIdentifier,
                    "name" => ClaimTypes.Name,
                    "role" => ClaimTypes.Role,
                    _ => key
                };

                if (value.ValueKind == JsonValueKind.Array)
                    foreach (var item in value.EnumerateArray())
                        claims.Add(new Claim(claimType, item.GetString() ?? ""));
                else
                    claims.Add(new Claim(claimType, value.ToString()));
            }

            return claims;
        }
        catch
        {
            return null;
        }
    }
}
