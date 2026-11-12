using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

public sealed class CatalogProductQueryService(FixtureCatalogRepository repository)
{
    public CatalogQueryResult Query(Guid storeId, string query)
    {
        ArgumentNullException.ThrowIfNull(repository);
        if (storeId == Guid.Empty) throw new ArgumentException("A valid store is required.", nameof(storeId));
        if (string.IsNullOrWhiteSpace(query))
            return new CatalogQueryResult(CatalogQueryResolutionStatus.NotFound, null, null, [], null);

        var hits = repository.Search(storeId, query);
        if (hits.Count == 0)
            return new CatalogQueryResult(CatalogQueryResolutionStatus.NotFound, null, null, [], null);

        if (hits.Count > 1)
        {
            var candidates = hits
                .Select(x => new CatalogProductCandidate(
                    x.Product.ProductId, x.Product.Name, x.Product.CategoryPath, x.Product.Unit))
                .ToArray();
            return new CatalogQueryResult(
                CatalogQueryResolutionStatus.ClarificationRequired,
                null,
                null,
                candidates,
                "چند محصول با این عبارت مطابقت دارند؛ لطفاً نام کامل یا کد محصول را مشخص کنید.");
        }

        var hit = hits[0];
        return new CatalogQueryResult(
            CatalogQueryResolutionStatus.Resolved,
            hit.Product,
            hit.Evidence,
            [],
            null);
    }
}
