using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FluxoCaixaDiario.Lancamentos.API.Middlewares;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Erro de validação: {Errors}", string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
            context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            context.Response.ContentType = "application/problem+json";

            var problem = new ValidationProblemDetails
            {
                Status = (int)HttpStatusCode.UnprocessableEntity,
                Title = "Erro de validação",
                Type = "https://fluxocaixa.api/errors/validation"
            };
            foreach (var error in ex.Errors)
                problem.Errors.TryAdd(error.PropertyName, [error.ErrorMessage]);

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado: {Message}", ex.Message);
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