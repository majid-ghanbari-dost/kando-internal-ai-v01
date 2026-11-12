using Kandoo.InternalAI.Core;
using Kandoo.InternalAI.Infrastructure;
using Xunit;

namespace Kandoo.InternalAI.Tests;

public sealed class CatalogMappingProposalExportTests
{
    private readonly CatalogMappingProposalExporter _exporter = new();

    [Fact]
    public void IdenticalProposalProducesByteIdenticalExportAndDigest()
    {
        var proposal = CreateProposal(SyntheticCatalogFixtures.PartyStoreId);
        var first = _exporter.Export(proposal);
        var second = _exporter.Export(proposal);

        Assert.Equal(first, second);
        Assert.Contains("\"contract_version\":1", first, StringComparison.Ordinal);
        Assert.Contains("\"content_sha256\":\"", first, StringComparison.Ordinal);
    }

    [Fact]
    public void ReorderedCollectionsProduceIdenticalCanonicalExport()
    {
        var proposal = CreateProposal(SyntheticCatalogFixtures.PartyStoreId);
        var reordered = proposal with
        {
            Mappings = proposal.Mappings.Reverse().ToArray(),
            QuarantinedFields = proposal.QuarantinedFields.Reverse().ToArray(),
            ExcludedSensitiveFields = proposal.ExcludedSensitiveFields.Reverse().ToArray(),
            ValidationIssues = proposal.ValidationIssues.Reverse().ToArray()
        };

        Assert.Equal(_exporter.Export(proposal), _exporter.Export(reordered));
    }

    [Fact]
    public void ImportVerifiesDigestAndPreservesEveryProposalField()
    {
        var proposal = CreateProposal(SyntheticCatalogFixtures.GroceryStoreId) with
        {
            Status = CatalogMappingStatus.Invalidated,
            HasSchemaDrift = true
        };
        var imported = _exporter.ImportAndVerify(_exporter.Export(proposal));

        Assert.Equal(proposal.StoreId, imported.StoreId);
        Assert.Equal(proposal.ProfileId, imported.ProfileId);
        Assert.Equal(proposal.ProposedProfileVersion, imported.ProposedProfileVersion);
        Assert.Equal(proposal.SourceSchemaVersion, imported.SourceSchemaVersion);
        Assert.Equal(proposal.Status, imported.Status);
        Assert.Equal(proposal.HasSchemaDrift, imported.HasSchemaDrift);
        Assert.Equal(proposal.Mappings.OrderBy(x => x.SourceField), imported.Mappings);
        Assert.Equal(proposal.QuarantinedFields.OrderBy(x => x, StringComparer.Ordinal), imported.QuarantinedFields);
        Assert.Equal(proposal.ExcludedSensitiveFields.OrderBy(x => x, StringComparer.Ordinal), imported.ExcludedSensitiveFields);
        Assert.Equal(proposal.ValidationIssues.OrderBy(x => x, StringComparer.Ordinal), imported.ValidationIssues);
    }

    [Fact]
    public void ImportRejectsTamperedDigestOrProposalContent()
    {
        var json = _exporter.Export(CreateProposal(SyntheticCatalogFixtures.PartyStoreId));
        var tampered = json.Replace("\"profile_id\":\"party-supply\"", "\"profile_id\":\"other-store\"", StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(() => _exporter.ImportAndVerify(tampered));
    }

    [Fact]
    public void ImportRejectsUnknownAndDuplicateProperties()
    {
        var json = _exporter.Export(CreateProposal(SyntheticCatalogFixtures.PartyStoreId));
        var unknown = json.Replace("\"contract\":", "\"unexpected\":true,\"contract\":", StringComparison.Ordinal);
        var duplicate = json.Replace("\"contract\":", "\"contract\":\"x\",\"contract\":", StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(() => _exporter.ImportAndVerify(unknown));
        Assert.Throws<InvalidDataException>(() => _exporter.ImportAndVerify(duplicate));
    }

    [Fact]
    public void SchemaDriftRemainsInvalidatedAfterExportAndImport()
    {
        var proposal = CreateProposal(SyntheticCatalogFixtures.GroceryStoreId) with
        {
            Status = CatalogMappingStatus.Invalidated,
            HasSchemaDrift = true,
            ValidationIssues = ["Schema drift detected: expected grocery-schema-v3, received grocery-schema-v4."]
        };

        var imported = _exporter.ImportAndVerify(_exporter.Export(proposal));

        Assert.True(imported.HasSchemaDrift);
        Assert.Equal(CatalogMappingStatus.Invalidated, imported.Status);
        Assert.Contains(imported.ValidationIssues, x => x.Contains("Schema drift", StringComparison.Ordinal));
    }

    [Fact]
    public void ExportContainsOnlyFieldMetadataAndNeverSyntheticRowValues()
    {
        var proposal = CreateProposal(SyntheticCatalogFixtures.PartyStoreId);
        var json = _exporter.Export(proposal);

        Assert.Contains("CustomerPhone", json, StringComparison.Ordinal);
        Assert.DoesNotContain("09120000000", json, StringComparison.Ordinal);
        Assert.DoesNotContain("5551234", json, StringComparison.Ordinal);
        Assert.DoesNotContain("بادکنک لاتکس قرمز", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("party-supply")]
    [InlineData("grocery")]
    [InlineData("clothing")]
    public void AllThreeSyntheticStoreProfilesProduceVerifiableReviewArtifacts(string profileId)
    {
        var profile = Assert.Single(SyntheticCatalogFixtures.Profiles, x => x.ProfileId == profileId);
        var proposal = CreateProposal(profile.StoreId);
        var imported = _exporter.ImportAndVerify(_exporter.Export(proposal));

        Assert.Equal(profile.StoreId, imported.StoreId);
        Assert.Equal(profile.ProfileId, imported.ProfileId);
        Assert.Equal(CatalogMappingStatus.Proposed, imported.Status);
    }

    [Fact]
    public void ExportRejectsNonPublicMappingCandidate()
    {
        var proposal = CreateProposal(SyntheticCatalogFixtures.PartyStoreId);
        var invalid = proposal with
        {
            Mappings = [new FieldMappingProposal("CustomerPhone", "customer_phone", CatalogValueType.String, CatalogSensitivity.Personal)]
        };

        Assert.Throws<InvalidDataException>(() => _exporter.Export(invalid));
    }

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
