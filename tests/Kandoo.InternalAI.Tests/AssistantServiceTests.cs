using Kandoo.InternalAI.Application;
using Kandoo.InternalAI.Core;
using Kandoo.InternalAI.Infrastructure;

namespace Kandoo.InternalAI.Tests;

public sealed class AssistantServiceTests
{
    private static readonly Guid StoreA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StoreB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
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
        Assert.Contains("داده آزمایشی", response.Answer);
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
        var service = new AssistantService(new FixedPlanProvider(new AssistantPlan("sql.execute", "SELECT * FROM products")),
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
