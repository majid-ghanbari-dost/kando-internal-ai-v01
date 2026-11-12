namespace Kandoo.InternalAI.Core;

public enum AssistantIntent
{
    ProductDetails,
    CurrentPrice,
    CurrentStock
}

public sealed record AssistantRequest(Guid StoreId, string Question);
public sealed record AssistantPlan(string ToolName, string Query, AssistantIntent Intent);
public sealed record ProductReadModel(Guid StoreId, string ProductId, string Name, string Description, string Unit, long CurrentPriceRials, decimal CurrentStock, DateTimeOffset ObservedAtUtc, string SourceId);
public sealed record ProductAnswer(string ProductId, string Name, string Description, string Unit, long CurrentPriceRials, decimal CurrentStock, DateTimeOffset ObservedAtUtc, string SourceId);
public sealed record Evidence(string SourceId, string ProductId, string ProductName, DateTimeOffset ObservedAtUtc);
public sealed record ProductCandidate(string ProductId, string Name, string Description, string Unit);
public sealed record AssistantResponse(
    string Answer,
    bool HasGroundedData,
    string DataMode,
    string Intent,
    IReadOnlyList<ProductAnswer> Products,
    IReadOnlyList<Evidence> Evidence,
    string ResolutionStatus = "resolved",
    string? ClarificationQuestion = null,
    IReadOnlyList<ProductCandidate>? Candidates = null);
public sealed record ProductLookupArguments(string Query);
public sealed record ProductLookupResult(IReadOnlyList<ProductReadModel> Products);
