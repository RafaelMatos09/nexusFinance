using System.Net;
using System.Net.Http.Json;
using NexusFinance.Api.Tests.Support;
using NexusFinance.Application.Health;

namespace NexusFinance.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.DatabaseReachable = true;
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/api/health")]
    public async Task Health_returns_ok_when_database_is_reachable(string path)
    {
        _factory.DatabaseReachable = true;
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthStatusDto>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal("ok", body.Database);
    }

    [Fact]
    public async Task Health_returns_service_unavailable_when_database_is_down()
    {
        _factory.DatabaseReachable = false;
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthStatusDto>();
        Assert.NotNull(body);
        Assert.Equal("degraded", body.Status);
        Assert.Equal("unavailable", body.Database);
    }

    [Fact]
    public async Task Health_allows_frontend_origin()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal("http://localhost:5173", Assert.Single(origins));
    }
}
