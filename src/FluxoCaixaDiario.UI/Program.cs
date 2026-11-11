using FluxoCaixaDiario.UI;
using FluxoCaixaDiario.UI.Auth;
using FluxoCaixaDiario.UI.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Autenticação
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<JwtAuthorizationHandler>();
builder.Services.AddAuthorizationCore();

var lancamentosUrl = builder.Configuration["ApiSettings:LancamentosBaseUrl"] ?? "http://localhost:5010";
var consolidadoUrl = builder.Configuration["ApiSettings:ConsolidadoBaseUrl"] ?? "http://localhost:5011";

// AuthService usa o HttpClient da Lancamentos API (onde está o endpoint /auth/login)
builder.Services.AddHttpClient<AuthService>(client =>
{
    client.BaseAddress = new Uri(lancamentosUrl);
});

builder.Services.AddHttpClient<LancamentosApiService>(client =>
{
    client.BaseAddress = new Uri(lancamentosUrl);
}).AddHttpMessageHandler<JwtAuthorizationHandler>();

builder.Services.AddHttpClient<ConsolidadoApiService>(client =>
{
    client.BaseAddress = new Uri(consolidadoUrl);
}).AddHttpMessageHandler<JwtAuthorizationHandler>();

await builder.Build().RunAsync();

