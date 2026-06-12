using System.Net;

namespace FluxoCaixaDiario.Consolidado.API.Middlewares;

public sealed class ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyAuthMiddleware> logger)
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    private static readonly string[] _exemptPaths =
    [
        "/health",
        "/metrics",
        "/swagger",
        "/favicon.ico"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (_exemptPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var configuredKey = configuration["ApiKey:Key"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            logger.LogWarning("ApiKey não configurada — requisição bloqueada por segurança.");
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            await context.Response.WriteAsync("Serviço sem configuração de segurança.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var receivedKey)
            || !string.Equals(receivedKey, configuredKey, StringComparison.Ordinal))
        {
            logger.LogWarning("Tentativa de acesso sem API Key válida. IP: {RemoteIp}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("API Key ausente ou inválida.");
            return;
        }

        await next(context);
    }
}