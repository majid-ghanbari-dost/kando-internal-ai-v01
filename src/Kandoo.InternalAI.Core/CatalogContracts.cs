namespace Kandoo.InternalAI.Core;

public enum CatalogValueType { String, Decimal, Integer, Boolean, Date }
public enum CatalogMappingStatus { Proposed, Approved, Rejected, Invalidated }
public enum CatalogAliasStatus { Proposed, Approved, Rejected }
public enum CatalogSensitivity { Public, Personal, Sensitive }

public sealed record StoreCatalogProfile(
    Guid StoreId,
    string ProfileId,
    int ProfileVersion,
    string SourceSchemaVersion,
    CatalogMappingStatus MappingStatus,
    string MappingProvenance,
    DateTimeOffset CreatedAtUtc);

public sealed record CatalogProduct(
    Guid StoreId,
    string ProductId,
    string Name,
    string NormalizedName,
    string CategoryPath,
    string Unit,
    bool IsActive,
    string SourceVersion,
    IReadOnlyDictionary<string, CatalogAttributeValue> Attributes);

public sealed record ProductAttributeDefinition(
    string Key,
    string DisplayName,
    CatalogValueType ValueType,
    CatalogSensitivity Sensitivity,
    IReadOnlyList<string> AllowedValues,
    bool IsAllowlisted);

public sealed record CatalogAttributeValue(
    string Key,
    CatalogValueType ValueType,
    string? StringValue,
    decimal? DecimalValue,
    long? IntegerValue,
    bool? BooleanValue,
    DateOnly? DateValue,
    string SourceField,
    DateTimeOffset ObservedAtUtc);

public sealed record ProductAlias(
    Guid StoreId,
    string ProductId,
    string NormalizedAlias,
    string Locale,
    string Source,
    decimal Confidence,
    CatalogAliasStatus ApprovalStatus);

public sealed record SourceFieldDefinition(string Name, string DataType);

public sealed record FieldMappingProposal(
    string SourceField,
    string TargetAttributeKey,
    CatalogValueType TargetValueType,
    CatalogSensitivity Sensitivity);

public sealed record CatalogMappingProposal(
    Guid StoreId,
    string ProfileId,
    int ProposedProfileVersion,
    string SourceSchemaVersion,
    CatalogMappingStatus Status,
    bool HasSchemaDrift,
    IReadOnlyList<FieldMappingProposal> Mappings,
    IReadOnlyList<string> QuarantinedFields,
    IReadOnlyList<string> ExcludedSensitiveFields,
    IReadOnlyList<string> ValidationIssues);

public sealed record CatalogSearchEvidence(
    string SourceId,
    string ProfileId,
    string SourceVersion,
    IReadOnlyList<string> MatchedAliases);

public sealed record CatalogSearchHit(CatalogProduct Product, CatalogSearchEvidence Evidence);

public sealed record CatalogAttributeValidationResult(Guid StoreId, string ProductId, bool IsValid, IReadOnlyList<string> Issues);

public enum CatalogQueryResolutionStatus { Resolved, ClarificationRequired, NotFound }
public sealed record CatalogProductCandidate(string ProductId, string Name, string CategoryPath, string Unit);
public sealed record CatalogQueryResult(CatalogQueryResolutionStatus Status, CatalogProduct? Product, CatalogSearchEvidence? Evidence, IReadOnlyList<CatalogProductCandidate> Candidates, string? ClarificationQuestion);
