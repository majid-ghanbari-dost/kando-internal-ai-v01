using Kandoo.InternalAI.Core;
using Kandoo.InternalAI.Infrastructure;
using Xunit;

namespace Kandoo.InternalAI.Tests;

public sealed class CatalogMappingProposalExampleFixtureTests
{
    [Theory]
    [InlineData("catalog-mapping-proposal-party.v1.json", "party-supply", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    [InlineData("catalog-mapping-proposal-grocery.v1.json", "grocery", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
    [InlineData("catalog-mapping-proposal-clothing.v1.json", "clothing", "cccccccc-cccc-cccc-cccc-cccccccccccc")]
    public void CheckedInSyntheticExampleImportsAndVerifies(string fileName, string profileId, string storeId)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var json = File.ReadAllText(path);
        var proposal = new CatalogMappingProposalExporter().ImportAndVerify(json);

        Assert.Equal(profileId, proposal.ProfileId);
        Assert.Equal(Guid.Parse(storeId), proposal.StoreId);
        Assert.Equal(CatalogMappingStatus.Proposed, proposal.Status);
        Assert.False(proposal.HasSchemaDrift);
    }
}
