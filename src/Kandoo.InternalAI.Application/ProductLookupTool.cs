using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Application;

public sealed class ProductLookupTool(IStoreReadGateway gateway) : IAssistantTool
{
    public const string ToolName = "product.lookup";
    public string Name => ToolName;

    public async Task<ProductLookupResult> ExecuteAsync(Guid storeId, ProductLookupArguments arguments, CancellationToken cancellationToken)
    {
        if (storeId == Guid.Empty) throw new ArgumentException("A valid store is required.", nameof(storeId));
        if (string.IsNullOrWhiteSpace(arguments.Query)) return new ProductLookupResult([]);
        var products = await gateway.FindProductsAsync(storeId, arguments.Query.Trim(), cancellationToken);
        return new ProductLookupResult(products);
    }
}
