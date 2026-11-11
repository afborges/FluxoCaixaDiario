using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FluxoCaixaDiario.Consolidado.Domain.Entities;
using FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;
using FluxoCaixaDiario.Consolidado.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluxoCaixaDiario.Consolidado.IntegrationTests.Controllers;

public sealed class ConsolidadoControllerTests(ConsolidadoApiFactory factory)
    : IClassFixture<ConsolidadoApiFactory>
{
    private HttpClient CriarCliente(string? apiKey = "test-api-key")
    {
        var client = factory.CreateClient();
        if (apiKey is not null)
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }

    private async Task SeedConsolidadoAsync(DateOnly data, decimal credito, decimal debito)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
        var consolidado = ConsolidadoDiario.Criar(data);
        if (credito > 0) consolidado.AplicarLancamento("Credito", credito);
        if (debito > 0) consolidado.AplicarLancamento("Debito", debito);
        db.ConsolidadosDiarios.Add(consolidado);
        await db.SaveChangesAsync();
    }

    // ─── GET /api/consolidado/{data} ────────────────────────────────────────

    [Fact]
    public async Task ObterPorData_SemApiKey_RetornaUnauthorized()
    {
        var client = CriarCliente(apiKey: null);

        var response = await client.GetAsync("/api/consolidado/2024-01-15");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ObterPorData_NaoExistente_RetornaNotFound()
    {
        var client = CriarCliente();
        var dataFutura = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)).ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/consolidado/{dataFutura}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ObterPorData_Existente_RetornaOkComSaldoCorreto()
    {
        var data = new DateOnly(2024, 3, 15);
        await SeedConsolidadoAsync(data, credito: 3000m, debito: 800m);
        var client = CriarCliente();

        var response = await client.GetAsync($"/api/consolidado/{data:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCreditos").GetDecimal().Should().Be(3000m);
        body.GetProperty("totalDebitos").GetDecimal().Should().Be(800m);
        body.GetProperty("saldoFinal").GetDecimal().Should().Be(2200m);
        body.GetProperty("quantidadeLancamentos").GetInt32().Should().Be(2);
    }

    // ─── GET /api/consolidado (histórico) ───────────────────────────────────

    [Fact]
    public async Task ObterHistorico_DatasValidas_RetornaOkComLista()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/consolidado?dataInicio=2024-01-01&dataFim=2024-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ObterHistorico_DataFimMenorQueInicio_RetornaBadRequest()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/consolidado?dataInicio=2024-01-31&dataFim=2024-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ObterHistorico_ComDadosSemeados_RetornaRegistros()
    {
        var data1 = new DateOnly(2024, 6, 1);
        var data2 = new DateOnly(2024, 6, 2);
        await SeedConsolidadoAsync(data1, credito: 1000m, debito: 200m);
        await SeedConsolidadoAsync(data2, credito: 500m, debito: 100m);
        var client = CriarCliente();

        var response = await client.GetAsync("/api/consolidado?dataInicio=2024-06-01&dataFim=2024-06-30");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }
}
