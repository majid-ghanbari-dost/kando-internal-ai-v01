using Kandoo.InternalAI.Application;
using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

public sealed class FakeStoreReadGateway : IStoreReadGateway
{
    private readonly IReadOnlyList<ProductReadModel> _products;
    public FakeStoreReadGateway()
    {
        var observedAt = new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
        var storeA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var storeB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        _products =
        [
            new(storeA, "P-100", "بادکنک لاتکس قرمز", "بسته ۱۰۰ عددی", "بسته", 250000, 12, observedAt, "fixture:store-a:products:v1"),
            new(storeA, "P-101", "بادکنک لاتکس آبی", "بسته ۱۰۰ عددی", "بسته", 260000, 8, observedAt, "fixture:store-a:products:v1"),
            new(storeB, "P-100", "روبان ساتن", "عرض ۲ سانتی‌متر", "حلقه", 180000, 20, observedAt, "fixture:store-b:products:v1")
        ];
    }
    public Task<IReadOnlyList<ProductReadModel>> FindProductsAsync(Guid storeId, string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = query.Trim();
        IReadOnlyList<ProductReadModel> matches = _products.Where(p => p.StoreId == storeId)
            .Where(p => p.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) || p.ProductId.Contains(normalized, StringComparison.OrdinalIgnoreCase)).ToArray();
        return Task.FromResult(matches);
    }
}
