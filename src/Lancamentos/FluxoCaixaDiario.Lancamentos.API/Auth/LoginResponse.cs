namespace FluxoCaixaDiario.Lancamentos.API.Auth;

public sealed record LoginResponse(string Token, string Username, string NomeCompleto, string Role, DateTime Expiracao);
