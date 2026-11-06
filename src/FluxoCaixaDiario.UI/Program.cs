using FluxoCaixaDiario.UI;
using FluxoCaixaDiario.UI.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var lancamentosUrl = builder.Configuration["ApiSettings:LancamentosBaseUrl"] ?? "http://localhost:5010";
var consolidadoUrl = builder.Configuration["ApiSettings:ConsolidadoBaseUrl"] ?? "http://localhost:5011";
var apiKey = builder.Configuration["ApiSettings:ApiKey"] ?? string.Empty;

builder.Services.AddHttpClient<LancamentosApiService>(client =>
{
    client.BaseAddress = new Uri(lancamentosUrl);
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
});

builder.Services.AddHttpClient<ConsolidadoApiService>(client =>
{
    client.BaseAddress = new Uri(consolidadoUrl);
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
});

await builder.Build().RunAsync();
