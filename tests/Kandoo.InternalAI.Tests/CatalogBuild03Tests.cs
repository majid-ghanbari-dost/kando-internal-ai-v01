using Kandoo.InternalAI.Core;
using Kandoo.InternalAI.Infrastructure;
using Xunit;

namespace Kandoo.InternalAI.Tests;

public sealed class CatalogBuild03Tests
{
    [Fact]
    public void ThreeDifferentStoreTypesShareOneCanonicalProductContract()
    {
        var fixtures = SyntheticCatalogFixtures.Products;
        Assert.Contains(fixtures, x => x.StoreId == SyntheticCatalogFixtures.PartyStoreId && x.Attributes.ContainsKey("material"));
        Assert.Contains(fixtures, x => x.StoreId == SyntheticCatalogFixtures.GroceryStoreId && x.Attributes.ContainsKey("weight_kg"));
        Assert.Contains(fixtures, x => x.StoreId == SyntheticCatalogFixtures.ClothingStoreId && x.Attributes.ContainsKey("size"));
        Assert.Equal(3, SyntheticCatalogFixtures.Profiles.Count);
        Assert.All(fixtures, product => Assert.NotEqual(Guid.Empty, product.StoreId));
    }

    [Fact]
    public void TypedAttributesRoundTripWithoutStoreSpecificProductTypes()
    {
        var grocery = Assert.Single(SyntheticCatalogFixtures.Products, x => x.ProductId == "G-100");
        var weight = grocery.Attributes["weight_kg"];
        Assert.Equal(CatalogValueType.Decimal, weight.ValueType);
        Assert.Equal(10m, weight.DecimalValue);

        var clothing = Assert.Single(SyntheticCatalogFixtures.Products, x => x.ProductId == "C-101");
        Assert.Equal(32L, clothing.Attributes["waist_size"].IntegerValue);
    }

    [Fact]
    public void MappingProposalQuarantinesUnknownFieldsAndExcludesPii()
    {
        var profile = Assert.Single(SyntheticCatalogFixtures.Profiles, x => x.StoreId == SyntheticCatalogFixtures.PartyStoreId);
        var definitions = new Dictionary<string, ProductAttributeDefinition>
        {
            ["Color"] = new("color", "رنگ", CatalogValueType.String, CatalogSensitivity.Public, [], true),
            ["DiameterCm"] = new("diameter_cm", "قطر", CatalogValueType.Decimal, CatalogSensitivity.Public, [], true)
        };
        var proposal = new CatalogMappingValidator().CreateProposal(
            profile,
            profile.SourceSchemaVersion,
            [
                new("Color", "string"),
                new("DiameterCm", "decimal"),
                new("InternalNote", "text"),
                new("CustomerPhone", "string")
            ],
            definitions);

        Assert.Equal(CatalogMappingStatus.Proposed, proposal.Status);
        Assert.Equal(2, proposal.Mappings.Count);
        Assert.Contains("InternalNote", proposal.QuarantinedFields);
        Assert.Contains("CustomerPhone", proposal.ExcludedSensitiveFields);
        Assert.DoesNotContain(proposal.ExcludedSensitiveFields, x => x.Contains("InternalNote", StringComparison.Ordinal));
    }

    [Fact]
    public void SchemaDriftInvalidatesProposalAndRecordsIssue()
    {
        var profile = Assert.Single(SyntheticCatalogFixtures.Profiles, x => x.StoreId == SyntheticCatalogFixtures.GroceryStoreId);
        var proposal = new CatalogMappingValidator().CreateProposal(
            profile, "grocery-schema-v4", [new("BrandName", "string")],
            new Dictionary<string, ProductAttributeDefinition>
            {
                ["BrandName"] = new("brand", "برند", CatalogValueType.String, CatalogSensitivity.Public, [], true)
            });

        Assert.True(proposal.HasSchemaDrift);
        Assert.Equal(CatalogMappingStatus.Invalidated, proposal.Status);
        Assert.Contains(proposal.ValidationIssues, x => x.Contains("Schema drift", StringComparison.Ordinal));
    }

    [Fact]
    public void SearchIsStoreScopedAndOnlyUsesApprovedAliases()
    {
        var repository = new FixtureCatalogRepository();
        var partyHit = Assert.Single(repository.Search(SyntheticCatalogFixtures.PartyStoreId, "بادکنک قرمز"));
        Assert.Equal("P-100", partyHit.Product.ProductId);
        Assert.Empty(repository.Search(SyntheticCatalogFixtures.GroceryStoreId, "بادکنک قرمز"));
        Assert.Empty(repository.Search(SyntheticCatalogFixtures.PartyStoreId, "بادکنک سرخ"));
    }

    [Fact]
    public void SearchByNameAndCodeIsDeterministic()
    {
        var repository = new FixtureCatalogRepository();
        var first = repository.Search(SyntheticCatalogFixtures.PartyStoreId, "بادکنک");
        var second = repository.Search(SyntheticCatalogFixtures.PartyStoreId, "بادکنک");
        Assert.Equal(first.Select(x => x.Product.ProductId), second.Select(x => x.Product.ProductId));
        Assert.Equal(new[] { "P-100", "P-101" }, first.Select(x => x.Product.ProductId));
    }

    [Fact]
    public void DescriptiveCatalogContractContainsNoLiveOperationalFields()
    {
        var names = typeof(CatalogProduct).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(names, x => x.Contains("Price", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, x => x.Contains("Stock", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, x => x.Contains("Invoice", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StoreWithUnapprovedMappingCannotBeSearched()
    {
        var profile = SyntheticCatalogFixtures.Profiles[0] with { MappingStatus = CatalogMappingStatus.Proposed };
        var repository = new FixtureCatalogRepository(
            [profile], SyntheticCatalogFixtures.Products, SyntheticCatalogFixtures.Aliases);
        Assert.Empty(repository.Search(profile.StoreId, "بادکنک قرمز"));
    }

    [Fact]
    public void EverySyntheticProductPassesItsOwnStoreAttributeSchema()
    {
        var validator = new CatalogAttributeSchemaValidator();
        foreach (var product in SyntheticCatalogFixtures.Products)
        {
            var result = validator.Validate(product, SyntheticCatalogFixtures.AttributeDefinitions[product.StoreId]);
            Assert.True(result.IsValid, string.Join("; ", result.Issues));
        }
    }

    [Fact]
    public void AttributeValidatorRejectsWrongTypeAndMultipleTypedPayloads()
    {
        var product = Assert.Single(SyntheticCatalogFixtures.Products, x => x.ProductId == "P-100");
        var invalidAttributes = product.Attributes.ToDictionary(x => x.Key, x => x.Value);
        invalidAttributes["diameter_cm"] = new CatalogAttributeValue(
            "diameter_cm", CatalogValueType.String, "30", 30m, null, null, null, "DiameterCm", DateTimeOffset.UtcNow);
        var invalidProduct = product with { Attributes = invalidAttributes };

        var result = new CatalogAttributeSchemaValidator().Validate(
            invalidProduct, SyntheticCatalogFixtures.AttributeDefinitions[product.StoreId]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Contains("type does not match", StringComparison.Ordinal));
        Assert.Contains(result.Issues, x => x.Contains("exactly one value", StringComparison.Ordinal));
    }

    [Fact]
    public void AttributeValidatorRejectsValuesOutsideStoreDefinedAllowedValues()
    {
        var product = Assert.Single(SyntheticCatalogFixtures.Products, x => x.ProductId == "P-100");
        var invalidAttributes = product.Attributes.ToDictionary(x => x.Key, x => x.Value);
        invalidAttributes["color"] = invalidAttributes["color"] with { StringValue = "بنفش" };
        var invalidProduct = product with { Attributes = invalidAttributes };

        var result = new CatalogAttributeSchemaValidator().Validate(
            invalidProduct, SyntheticCatalogFixtures.AttributeDefinitions[product.StoreId]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Contains("outside its allowed values", StringComparison.Ordinal));
    }

    [Fact]
    public void AttributeValidatorRejectsSensitiveOrNonAllowlistedDefinitions()
    {
        var product = Assert.Single(SyntheticCatalogFixtures.Products, x => x.ProductId == "P-100");
        var definitions = SyntheticCatalogFixtures.AttributeDefinitions[product.StoreId].ToDictionary(x => x.Key, x => x.Value);
        definitions["color"] = definitions["color"] with { Sensitivity = CatalogSensitivity.Personal, IsAllowlisted = false };

        var result = new CatalogAttributeSchemaValidator().Validate(product, definitions);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Contains("not approved", StringComparison.Ordinal));
    }

}
