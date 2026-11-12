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
        var request = new AssistantRequest(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "بادکنک لاتکس قرمز");
        var response = await _client.PostAsJsonAsync("/assistant/ask", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AssistantResponse>();
        Assert.NotNull(body);
        Assert.True(body.HasGroundedData);
        Assert.Equal("fixture", body.DataMode);
        Assert.Contains(body.Evidence, item => item.ProductId == "P-100");
    }
    [Fact]
    public async Task AskEndpoint_ExposesStructuredClarificationForAmbiguousProduct()
    {
        var request = new AssistantRequest(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "قیمت بادکنک");
        var response = await _client.PostAsJsonAsync("/assistant/ask", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AssistantResponse>();
        Assert.NotNull(body);
        Assert.Equal("clarification_required", body.ResolutionStatus);
        Assert.Empty(body.Products);
        Assert.Equal(2, body.Candidates!.Count);
        Assert.NotNull(body.ClarificationQuestion);
    }
    [Fact]
    public async Task AskEndpoint_CatalogQueryReturnsTypedAttributesAndEvidence()
    {
        var request = new AssistantRequest(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "قطر بادکنک فویلی ستاره‌ای چقدر است؟");
        var response = await _client.PostAsJsonAsync("/assistant/ask", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AssistantResponse>();
        Assert.NotNull(body);
        Assert.Equal("resolved", body.ResolutionStatus);
        Assert.Equal(nameof(AssistantIntent.CatalogAttributes), body.Intent);
        Assert.True(body.HasGroundedData);
        Assert.Equal("fixture", body.DataMode);
        Assert.Empty(body.Products);
        Assert.NotNull(body.CatalogProduct);
        Assert.Equal("P-101", body.CatalogProduct.ProductId);
        var diameter = Assert.Single(body.CatalogProduct.Attributes, item => item.Key == "diameter_cm");
        Assert.Equal(CatalogValueType.Decimal, diameter.ValueType);
        Assert.Equal(45m, diameter.DecimalValue);
        var shape = Assert.Single(body.CatalogProduct.Attributes, item => item.Key == "shape");
        Assert.Equal(CatalogValueType.String, shape.ValueType);
        Assert.Equal("ستاره", shape.StringValue);
        Assert.Single(body.Evidence);
        Assert.DoesNotContain("ریال", body.Answer);
        Assert.DoesNotContain("موجودی", body.Answer);
    }
    [Fact]
    public async Task AskEndpoint_CatalogQuerySupportsDifferentStoreAttributeSchemas()
    {
        var grocery = await _client.PostAsJsonAsync("/assistant/ask", new AssistantRequest(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "وزن برنج هاشمی چقدر است؟"));
        var groceryBody = await grocery.Content.ReadFromJsonAsync<AssistantResponse>();
        Assert.Equal(HttpStatusCode.OK, grocery.StatusCode);
        Assert.Equal("G-100", groceryBody!.CatalogProduct!.ProductId);
        Assert.Equal(10m, Assert.Single(groceryBody.CatalogProduct.Attributes, item => item.Key == "weight_kg").DecimalValue);
        var clothing = await _client.PostAsJsonAsync("/assistant/ask", new AssistantRequest(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "سایز پیراهن مردانه چیست؟"));
        var clothingBody = await clothing.Content.ReadFromJsonAsync<AssistantResponse>();
        Assert.Equal(HttpStatusCode.OK, clothing.StatusCode);
        Assert.Equal("C-100", clothingBody!.CatalogProduct!.ProductId);
        Assert.Equal("L", Assert.Single(clothingBody.CatalogProduct.Attributes, item => item.Key == "size").StringValue);
    }
    [Fact]
    public async Task AskEndpoint_CatalogAmbiguityDoesNotSelectProductOrReturnAttributes()
    {
        var response = await _client.PostAsJsonAsync("/assistant/ask", new AssistantRequest(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "جنس بادکنک چیست؟"));
        var body = await response.Content.ReadFromJsonAsync<AssistantResponse>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("clarification_required", body!.ResolutionStatus);
        Assert.Empty(body.Products);
        Assert.Null(body.CatalogProduct);
        Assert.Equal(2, body.Candidates!.Count);
        Assert.Empty(body.Evidence);
    }
    [Fact]
    public async Task AskEndpoint_CatalogQueryCannotCrossStoreBoundary()
    {
        var response = await _client.PostAsJsonAsync("/assistant/ask", new AssistantRequest(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "وزن برنج هاشمی چقدر است؟"));
        var body = await response.Content.ReadFromJsonAsync<AssistantResponse>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("not_found", body!.ResolutionStatus);
        Assert.Null(body.CatalogProduct);
        Assert.Empty(body.Evidence);
    }
    private sealed record HealthResponse(string Status, string Mode);
}
