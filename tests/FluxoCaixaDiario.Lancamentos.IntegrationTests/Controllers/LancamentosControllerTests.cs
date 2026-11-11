using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FluxoCaixaDiario.Lancamentos.IntegrationTests.Infrastructure;
using Xunit;

namespace FluxoCaixaDiario.Lancamentos.IntegrationTests.Controllers;

public sealed class LancamentosControllerTests(LancamentosApiFactory factory)
    : IClassFixture<LancamentosApiFactory>
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private HttpClient CriarCliente(string? apiKey = "test-api-key")
    {
        var client = factory.CreateClient();
        if (apiKey is not null)
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }

    // ─── POST /api/lancamentos ───────────────────────────────────────────────

    [Fact]
    public async Task Criar_ComDadosValidos_RetornaCreated()
    {
        var client = CriarCliente();
        var payload = new
        {
            tipo = "Credito",
            valor = 1500.00m,
            descricao = "Venda de produto",
            data = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        };

        var response = await client.PostAsJsonAsync("/api/lancamentos", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("tipo").GetString().Should().Be("Credito");
        body.GetProperty("valor").GetDecimal().Should().Be(1500.00m);
        body.GetProperty("status").GetString().Should().Be("Confirmado");
    }

    [Fact]
    public async Task Criar_SemApiKey_RetornaUnauthorized()
    {
        var client = CriarCliente(apiKey: null);
        var payload = new { tipo = "Credito", valor = 100m, descricao = "Teste", data = "2024-01-15" };

        var response = await client.PostAsJsonAsync("/api/lancamentos", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Criar_ComValorZero_RetornaUnprocessableEntity()
    {
        var client = CriarCliente();
        var payload = new { tipo = "Credito", valor = 0m, descricao = "Inválido", data = "2024-01-15" };

        var response = await client.PostAsJsonAsync("/api/lancamentos", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Criar_ComTipoInvalido_RetornaUnprocessableEntity()
    {
        var client = CriarCliente();
        var payload = new { tipo = "Invalido", valor = 100m, descricao = "Teste", data = "2024-01-15" };

        var response = await client.PostAsJsonAsync("/api/lancamentos", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ─── GET /api/lancamentos ────────────────────────────────────────────────

    [Fact]
    public async Task Listar_RetornaOkComPaginacao()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/lancamentos?pagina=1&tamanhoPagina=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("itens", out _).Should().BeTrue();
        body.TryGetProperty("total", out _).Should().BeTrue();
    }

    // ─── GET /api/lancamentos/{id} ───────────────────────────────────────────

    [Fact]
    public async Task ObterPorId_Existente_RetornaOk()
    {
        var client = CriarCliente();
        var createPayload = new
        {
            tipo = "Debito",
            valor = 250.00m,
            descricao = "Pagamento fornecedor",
            data = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        };
        var createResponse = await client.PostAsJsonAsync("/api/lancamentos", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var response = await client.GetAsync($"/api/lancamentos/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().Be(id);
        body.GetProperty("tipo").GetString().Should().Be("Debito");
    }

    [Fact]
    public async Task ObterPorId_Inexistente_RetornaNotFound()
    {
        var client = CriarCliente();
        var idAleatorio = Guid.NewGuid();

        var response = await client.GetAsync($"/api/lancamentos/{idAleatorio}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── DELETE /api/lancamentos/{id} ───────────────────────────────────────

    [Fact]
    public async Task Cancelar_Existente_RetornaNoContent()
    {
        var client = CriarCliente();
        var createPayload = new
        {
            tipo = "Credito",
            valor = 300.00m,
            descricao = "Lançamento para cancelar",
            data = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        };
        var createResponse = await client.PostAsJsonAsync("/api/lancamentos", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var response = await client.DeleteAsync($"/api/lancamentos/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancelar_Inexistente_RetornaNotFound()
    {
        var client = CriarCliente();
        var idAleatorio = Guid.NewGuid();

        var response = await client.DeleteAsync($"/api/lancamentos/{idAleatorio}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancelar_JaCancelado_RetornaUnprocessableEntity()
    {
        var client = CriarCliente();
        var createPayload = new
        {
            tipo = "Credito",
            valor = 100.00m,
            descricao = "Cancelar duas vezes",
            data = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        };
        var createResponse = await client.PostAsJsonAsync("/api/lancamentos", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        await client.DeleteAsync($"/api/lancamentos/{id}");
        var response = await client.DeleteAsync($"/api/lancamentos/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
