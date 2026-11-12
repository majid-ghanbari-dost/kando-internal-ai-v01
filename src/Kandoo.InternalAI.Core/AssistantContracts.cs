namespace Kandoo.InternalAI.Core;
public enum AssistantIntent { ProductDetails, CurrentPrice, CurrentStock, CatalogAttributes }
public sealed record AssistantRequest(Guid StoreId, string Question);
public sealed record AssistantPlan(string ToolName, string Query, AssistantIntent Intent);
public sealed record ProductReadModel(Guid StoreId, string ProductId, string Name, string Description, string Unit, long CurrentPriceRials, decimal CurrentStock, DateTimeOffset ObservedAtUtc, string SourceId);
public sealed record ProductAnswer(string ProductId, string Name, string Description, string Unit, long CurrentPriceRials, decimal CurrentStock, DateTimeOffset ObservedAtUtc, string SourceId);
public sealed record Evidence(string SourceId, string ProductId, string ProductName, DateTimeOffset ObservedAtUtc);
public sealed record ProductCandidate(string ProductId, string Name, string Description, string Unit);
public sealed record CatalogAttributeAnswer(string Key, string DisplayName, CatalogValueType ValueType, string? StringValue, decimal? DecimalValue, long? IntegerValue, bool? BooleanValue, DateOnly? DateValue, string SourceField);
public sealed record CatalogProductAnswer(string ProductId, string Name, string CategoryPath, string Unit, IReadOnlyList<CatalogAttributeAnswer> Attributes, string ProfileId, string SourceVersion);
public sealed record AssistantResponse(
    string Answer, bool HasGroundedData, string DataMode, string Intent,
    IReadOnlyList<ProductAnswer> Products, IReadOnlyList<Evidence> Evidence,
    string ResolutionStatus = "resolved", string? ClarificationQuestion = null,
    IReadOnlyList<ProductCandidate>? Candidates = null, CatalogProductAnswer? CatalogProduct = null);
public sealed record ProductLookupArguments(string Query);
public sealed record ProductLookupResult(IReadOnlyList<ProductReadModel> Products);
