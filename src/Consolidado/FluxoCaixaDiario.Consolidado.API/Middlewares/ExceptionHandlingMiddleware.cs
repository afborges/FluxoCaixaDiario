using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FluxoCaixaDiario.Consolidado.API.Middlewares;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado: {Message}", ex.Message);

            if (context.Response.HasStarted)
            {
                logger.LogWarning("A resposta já foi iniciada; não é possível retornar um erro estruturado.");
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Erro interno do servidor",
                Type = "https://fluxocaixa.api/errors/internal"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
