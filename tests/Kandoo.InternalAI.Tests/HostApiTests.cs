using System.Net;
using System.Net.Http.Json;
using Kandoo.InternalAI.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kandoo.InternalAI.Tests;

public sealed class HostApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HostApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task HealthEndpoint_ReportsFixtureMode()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal("fixture", body.Mode);
    }

    [Fact]
    public async Task AskEndpoint_ReturnsFixtureEvidence()
    {
        var request = new AssistantRequest(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "بادکنک لاتکس قرمز");

        var response = await _client.PostAsJsonAsync("/assistant/ask", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AssistantResponse>();
        Assert.NotNull(body);
        Assert.True(body.HasGroundedData);
        Assert.Equal("fixture", body.DataMode);
        Assert.Contains(body.Evidence, item => item.ProductId == "P-100");
    }

    private sealed record HealthResponse(string Status, string Mode);
}
