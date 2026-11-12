using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

public sealed class FixtureCatalogRepository
{
    private readonly IReadOnlyList<StoreCatalogProfile> _profiles;
    private readonly IReadOnlyList<CatalogProduct> _products;
    private readonly IReadOnlyList<ProductAlias> _aliases;

    public FixtureCatalogRepository(
        IReadOnlyList<StoreCatalogProfile>? profiles = null,
        IReadOnlyList<CatalogProduct>? products = null,
        IReadOnlyList<ProductAlias>? aliases = null)
    {
        _profiles = profiles ?? SyntheticCatalogFixtures.Profiles;
        _products = products ?? SyntheticCatalogFixtures.Products;
        _aliases = aliases ?? SyntheticCatalogFixtures.Aliases;
    }

    public IReadOnlyList<StoreCatalogProfile> GetProfiles(Guid storeId) =>
        _profiles.Where(x => x.StoreId == storeId).ToArray();

    public IReadOnlyList<CatalogSearchHit> Search(Guid storeId, string query)
    {
        if (storeId == Guid.Empty) throw new ArgumentException("A valid store is required.", nameof(storeId));
        var normalizedQuery = CatalogTextNormalizer.Normalize(query);
        if (normalizedQuery.Length == 0) return [];

        var profile = _profiles.SingleOrDefault(x => x.StoreId == storeId);
        if (profile is null || profile.MappingStatus != CatalogMappingStatus.Approved) return [];

        var storeProducts = _products.Where(x => x.StoreId == storeId && x.IsActive).ToArray();
        var approvedAliases = _aliases
            .Where(x => x.StoreId == storeId && x.ApprovalStatus == CatalogAliasStatus.Approved)
            .ToArray();

        var hits = new List<CatalogSearchHit>();
        foreach (var product in storeProducts)
        {
            var matchedAliases = approvedAliases
                .Where(x => x.ProductId == product.ProductId &&
                            (x.NormalizedAlias.Contains(normalizedQuery, StringComparison.Ordinal) ||
                             normalizedQuery.Contains(x.NormalizedAlias, StringComparison.Ordinal)))
                .Select(x => x.NormalizedAlias)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            var nameMatches = product.NormalizedName.Contains(normalizedQuery, StringComparison.Ordinal) ||
                              normalizedQuery.Contains(product.NormalizedName, StringComparison.Ordinal) ||
                              product.ProductId.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase);

            if (!nameMatches && matchedAliases.Length == 0) continue;

            var evidence = new CatalogSearchEvidence(
                $"fixture:{storeId:N}:catalog:v1",
                profile.ProfileId,
                product.SourceVersion,
                matchedAliases);
            hits.Add(new CatalogSearchHit(product, evidence));
        }

        return hits.OrderBy(x => x.Product.Name, StringComparer.Ordinal)
            .ThenBy(x => x.Product.ProductId, StringComparer.Ordinal)
            .ToArray();
    }
}
