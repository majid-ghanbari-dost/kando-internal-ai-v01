using Kandoo.InternalAI.Core;
using Kandoo.InternalAI.Infrastructure;
using Xunit;

namespace Kandoo.InternalAI.Tests;

public sealed class CatalogMappingReviewDecisionTests
{
    private static readonly DateTimeOffset ReviewedAt =
        new(2026, 11, 12, 16, 30, 0, TimeSpan.Zero);

    private readonly CatalogMappingProposalExporter _proposalExporter = new();
    private readonly CatalogMappingReviewDecisionExporter _decisionExporter = new();

    [Theory]
    [InlineData(CatalogMappingReviewDecision.Approved)]
    [InlineData(CatalogMappingReviewDecision.Rejected)]
    [InlineData(CatalogMappingReviewDecision.ChangesRequested)]
    public void EachExplicitDecisionExportsAndVerifiesAgainstExactProposal(CatalogMappingReviewDecision decision)
    {
        var proposalJson = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);
        var json = _decisionExporter.Export(proposalJson, decision, "fixture-reviewer-01", ReviewedAt, "Reviewed synthetic mapping.");
        var imported = _decisionExporter.ImportAndVerify(json, proposalJson);

        Assert.Equal(decision, imported.Decision);
        Assert.Equal("fixture-reviewer-01", imported.ReviewerRef);
        Assert.Equal(ReviewedAt, imported.ReviewedAtUtc);
        Assert.Equal("Reviewed synthetic mapping.", imported.Reason);
        Assert.StartsWith("cmrd-", imported.DecisionId, StringComparison.Ordinal);
        Assert.Equal(64, imported.DecisionContentSha256.Length);
    }

    [Fact]
    public void UnspecifiedDefaultDecisionIsRejectedRatherThanDefaultingToApproval()
    {
        Assert.Equal(CatalogMappingReviewDecision.Unspecified, default);
        var proposalJson = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);

        Assert.Throws<InvalidDataException>(() =>
            _decisionExporter.Export(proposalJson, default, "fixture-reviewer-01", ReviewedAt, "No decision supplied."));
    }

    [Fact]
    public void IdenticalDecisionInputsProduceByteIdenticalArtifactAndStableId()
    {
        var proposalJson = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);
        var first = _decisionExporter.Export(proposalJson, CatalogMappingReviewDecision.Approved,
            "fixture-reviewer-01", ReviewedAt, "Exact same review.");
        var second = _decisionExporter.Export(proposalJson, CatalogMappingReviewDecision.Approved,
            "fixture-reviewer-01", ReviewedAt, "Exact same review.");

        Assert.Equal(first, second);
        Assert.Equal(
            _decisionExporter.ImportAndVerify(first, proposalJson).DecisionId,
            _decisionExporter.ImportAndVerify(second, proposalJson).DecisionId);
    }

    [Fact]
    public void DifferentDecisionOutcomeProducesDifferentDecisionIdentity()
    {
        var proposalJson = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);
        var approved = _decisionExporter.Export(proposalJson, CatalogMappingReviewDecision.Approved,
            "fixture-reviewer-01", ReviewedAt, "Reviewed.");
        var rejected = _decisionExporter.Export(proposalJson, CatalogMappingReviewDecision.Rejected,
            "fixture-reviewer-01", ReviewedAt, "Reviewed.");

        Assert.NotEqual(
            _decisionExporter.ImportAndVerify(approved, proposalJson).DecisionId,
            _decisionExporter.ImportAndVerify(rejected, proposalJson).DecisionId);
    }

    [Fact]
    public void DecisionCannotBeReusedForChangedProposalContent()
    {
        var original = CreateProposal(SyntheticCatalogFixtures.PartyStoreId);
        var changed = original with { ValidationIssues = ["A newly discovered schema issue."] };
        var originalJson = _proposalExporter.Export(original);
        var changedJson = _proposalExporter.Export(changed);
        var decisionJson = _decisionExporter.Export(originalJson, CatalogMappingReviewDecision.Approved,
            "fixture-reviewer-01", ReviewedAt, "Reviewed.");

        Assert.Throws<InvalidDataException>(() => _decisionExporter.ImportAndVerify(decisionJson, changedJson));
    }

    [Fact]
    public void DecisionCannotBeReusedAcrossStoresOrProfiles()
    {
        var partyProposal = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);
        var groceryProposal = ExportProposal(SyntheticCatalogFixtures.GroceryStoreId);
        var decisionJson = _decisionExporter.Export(partyProposal, CatalogMappingReviewDecision.Approved,
            "fixture-reviewer-01", ReviewedAt, "Reviewed.");

        Assert.Throws<InvalidDataException>(() => _decisionExporter.ImportAndVerify(decisionJson, groceryProposal));
    }

    [Fact]
    public void DecisionRejectsTamperedReasonOrDigest()
    {
        var proposalJson = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);
        var json = _decisionExporter.Export(proposalJson, CatalogMappingReviewDecision.Rejected,
            "fixture-reviewer-01", ReviewedAt, "Reason before tampering.");
        var tampered = json.Replace("Reason before tampering.", "Reason after tampering.", StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(() => _decisionExporter.ImportAndVerify(tampered, proposalJson));
    }

    [Fact]
    public void DecisionRejectsUnknownAndDuplicateProperties()
    {
        var proposalJson = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);
        var json = _decisionExporter.Export(proposalJson, CatalogMappingReviewDecision.ChangesRequested,
            "fixture-reviewer-01", ReviewedAt, "Please revise.");
        var unknown = json.Replace("\"contract\":", "\"unexpected\":true,\"contract\":", StringComparison.Ordinal);
        var duplicate = json.Replace("\"contract\":", "\"contract\":\"x\",\"contract\":", StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(() => _decisionExporter.ImportAndVerify(unknown, proposalJson));
        Assert.Throws<InvalidDataException>(() => _decisionExporter.ImportAndVerify(duplicate, proposalJson));
    }

    [Fact]
    public void DecisionRejectsUnsupportedEnumValue()
    {
        var proposalJson = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);
        var json = _decisionExporter.Export(proposalJson, CatalogMappingReviewDecision.Approved,
            "fixture-reviewer-01", ReviewedAt, "Reviewed.");
        var tampered = json.Replace("\"decision\":\"Approved\"", "\"decision\":\"AutoApproved\"", StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(() => _decisionExporter.ImportAndVerify(tampered, proposalJson));
    }

    [Fact]
    public void ReviewerReferenceAndReasonAreRequiredAndBounded()
    {
        var proposalJson = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);

        Assert.Throws<InvalidDataException>(() => _decisionExporter.Export(proposalJson,
            CatalogMappingReviewDecision.Approved, " ", ReviewedAt, "Reviewed."));
        Assert.Throws<InvalidDataException>(() => _decisionExporter.Export(proposalJson,
            CatalogMappingReviewDecision.Approved, "fixture-reviewer-01", ReviewedAt, " "));
        Assert.Throws<InvalidDataException>(() => _decisionExporter.Export(proposalJson,
            CatalogMappingReviewDecision.Approved, "fixture-reviewer-01", ReviewedAt, new string('x', 2001)));
    }

    [Fact]
    public void ReviewTimestampMustBeNormalizedToUtc()
    {
        var proposalJson = ExportProposal(SyntheticCatalogFixtures.PartyStoreId);
        var nonUtc = new DateTimeOffset(2026, 11, 12, 19, 30, 0, TimeSpan.FromHours(3));

        Assert.Throws<InvalidDataException>(() => _decisionExporter.Export(proposalJson,
            CatalogMappingReviewDecision.Approved, "fixture-reviewer-01", nonUtc, "Reviewed."));
    }

    [Theory]
    [InlineData("party-supply")]
    [InlineData("grocery")]
    [InlineData("clothing")]
    public void ReviewDecisionRoundTripsForEverySyntheticStoreProfile(string profileId)
    {
        var profile = Assert.Single(SyntheticCatalogFixtures.Profiles, x => x.ProfileId == profileId);
        var proposalJson = ExportProposal(profile.StoreId);
        var json = _decisionExporter.Export(proposalJson, CatalogMappingReviewDecision.ChangesRequested,
            "fixture-reviewer-01", ReviewedAt, "Please review field mappings.");
        var imported = _decisionExporter.ImportAndVerify(json, proposalJson);

        Assert.Equal(profile.StoreId, imported.StoreId);
        Assert.Equal(profile.ProfileId, imported.ProfileId);
        Assert.Equal(CatalogMappingReviewDecision.ChangesRequested, imported.Decision);
    }

    private string ExportProposal(Guid storeId) => _proposalExporter.Export(CreateProposal(storeId));

    private static CatalogMappingProposal CreateProposal(Guid storeId)
    {
        var profile = Assert.Single(SyntheticCatalogFixtures.Profiles, x => x.StoreId == storeId);
        return new CatalogMappingValidator().CreateProposal(
            profile,
            profile.SourceSchemaVersion,
            [
                new("Color", "string"),
                new("DiameterCm", "decimal"),
                new("InternalNote", "text"),
                new("CustomerPhone", "string")
            ],
            new Dictionary<string, ProductAttributeDefinition>
            {
                ["Color"] = new("color", "رنگ", CatalogValueType.String, CatalogSensitivity.Public, [], true),
                ["DiameterCm"] = new("diameter_cm", "قطر", CatalogValueType.Decimal, CatalogSensitivity.Public, [], true)
            });
    }
}
