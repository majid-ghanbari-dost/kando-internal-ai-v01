namespace Kandoo.InternalAI.Core;

public sealed record AssistantRequest(Guid StoreId, string Question);
public sealed record AssistantPlan(string ToolName, string Query);
public sealed record ProductReadModel(Guid StoreId, string ProductId, string Name, string Description, string Unit, long CurrentPriceRials, decimal CurrentStock, DateTimeOffset ObservedAtUtc, string SourceId);
public sealed record Evidence(string SourceId, string ProductId, string ProductName, DateTimeOffset ObservedAtUtc);
public sealed record AssistantResponse(string Answer, bool HasGroundedData, string DataMode, IReadOnlyList<Evidence> Evidence);
public sealed record ProductLookupArguments(string Query);
public sealed record ProductLookupResult(IReadOnlyList<ProductReadModel> Products);
