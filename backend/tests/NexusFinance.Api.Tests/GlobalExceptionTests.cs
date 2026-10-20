using System.Net;
using System.Text.Json;

namespace NexusFinance.Api.Tests;

public sealed class GlobalExceptionTests : IClassFixture<Support.ApiFactory>
{
    private readonly Support.ApiFactory _factory;

    public GlobalExceptionTests(Support.ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unhandled_exception_returns_problem_details()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(500, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Erro interno do servidor", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("falha de teste", document.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/rota-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Recurso não encontrado", document.RootElement.GetProperty("title").GetString());
    }
}
