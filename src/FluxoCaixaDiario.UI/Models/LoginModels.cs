namespace FluxoCaixaDiario.UI.Models;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record LoginResponse(string Token, string Username, string NomeCompleto, string Role, DateTime Expiracao);
