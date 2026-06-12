namespace FluxoCaixaDiario.Consolidado.API.Middlewares;

public sealed class CacheControlMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Request.Method == HttpMethods.Get
            && context.Response.StatusCode == StatusCodes.Status200OK
            && !context.Response.Headers.ContainsKey("Cache-Control"))
        {
            context.Response.Headers.CacheControl = "private, max-age=300";
        }
    }
}