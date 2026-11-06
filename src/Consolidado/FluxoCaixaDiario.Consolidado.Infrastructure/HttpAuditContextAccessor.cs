using Microsoft.AspNetCore.Http;
using FluxoCaixaDiario.Consolidado.Application.Interfaces;

namespace FluxoCaixaDiario.Consolidado.Infrastructure;

public sealed class HttpAuditContextAccessor(IHttpContextAccessor httpContextAccessor)
    : IAuditContextAccessor
{
    public string? CorrelationId =>
        httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault();

    public string? UsuarioOuChave
    {
        get
        {
            var key = httpContextAccessor.HttpContext?.Request.Headers["X-Api-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(key)) return null;
            return $"key:...{key[^Math.Min(4, key.Length)..]}";
        }
    }
}