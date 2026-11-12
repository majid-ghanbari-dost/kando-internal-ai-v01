namespace Kandoo.InternalAI.Core;

public enum CatalogMappingReviewDecision { Approved, Rejected, ChangesRequested }

/// <summary>
/// A portable human-review record bound to one exact mapping proposal artifact.
/// ReviewerRef is opaque fixture metadata, not an authenticated identity.
/// </summary>
public sealed record CatalogMappingReviewDecisionRecord(
    string DecisionId,
    string ProposalId,
    string ProposalContentSha256,
    Guid StoreId,
    string ProfileId,
    CatalogMappingReviewDecision Decision,
    string ReviewerRef,
    DateTimeOffset ReviewedAtUtc,
    string Reason,
    string DecisionContentSha256);
