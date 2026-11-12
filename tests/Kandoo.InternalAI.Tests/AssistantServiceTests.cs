using Kandoo.InternalAI.Application;
using Kandoo.InternalAI.Core;
using Kandoo.InternalAI.Infrastructure;
using Xunit;

namespace Kandoo.InternalAI.Tests;

public sealed class AssistantServiceTests
{
    private static readonly Guid StoreA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static AssistantService CreateService() => new(new FakeModelProvider(),
        new AssistantToolRegistry([new ProductLookupTool(new FakeStoreReadGateway())]));

    [Fact]
    public async Task ProductLookup_ReturnsGroundedFixtureEvidence()
    {
        var response = await CreateService().AskAsync(new AssistantRequest(StoreA, "بادکنک لاتکس قرمز"), CancellationToken.None);
        Assert.True(response.HasGroundedData);
        Assert.Equal("fixture", response.DataMode);
        Assert.Single(response.Evidence);
        Assert.Equal("P-100", response.Evidence[0].ProductId);
        Assert.Single(response.Products);
        Assert.Equal(250000, response.Products[0].CurrentPriceRials);
        Assert.Contains("داده آزمایشی", response.Answer);
    }

    [Fact]
    public async Task PriceQuestion_ExtractsProductAndReturnsPriceIntent()
    {
        var response = await CreateService().AskAsync(
            new AssistantRequest(StoreA, "قیمت بادكنک لاتكس قرمز چنده؟"), CancellationToken.None);

        Assert.True(response.HasGroundedData);
        Assert.Equal(nameof(AssistantIntent.CurrentPrice), response.Intent);
        Assert.Equal("P-100", Assert.Single(response.Products).ProductId);
        Assert.Contains("ریال", response.Answer);
    }

    [Fact]
    public async Task ProductLookup_IsolatedByStoreId()
    {
        var response = await CreateService().AskAsync(new AssistantRequest(StoreA, "روبان ساتن"), CancellationToken.None);
        Assert.False(response.HasGroundedData);
        Assert.Empty(response.Evidence);
    }

    [Fact]
    public async Task UnknownTool_IsRejectedWithoutExecution()
    {
        var service = new AssistantService(new FixedPlanProvider(
                new AssistantPlan("sql.execute", "SELECT * FROM products", AssistantIntent.ProductDetails)),
            new AssistantToolRegistry([new ProductLookupTool(new FakeStoreReadGateway())]));
        var response = await service.AskAsync(new AssistantRequest(StoreA, "هر چیزی"), CancellationToken.None);
        Assert.False(response.HasGroundedData);
        Assert.Contains("ابزار مجاز", response.Answer);
    }

    [Fact]
    public async Task EmptyQuestion_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().AskAsync(new AssistantRequest(StoreA, " "), CancellationToken.None));
    }

    private sealed class FixedPlanProvider(AssistantPlan plan) : IAssistantModelProvider
    {
        public Task<AssistantPlan> CreatePlanAsync(string question, CancellationToken cancellationToken) => Task.FromResult(plan);
    }
}
