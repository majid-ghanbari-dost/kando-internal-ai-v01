using Kandoo.InternalAI.Application;
using Kandoo.InternalAI.Core;
namespace Kandoo.InternalAI.Infrastructure;
public sealed class CatalogQueryTool(CatalogProductQueryService queryService) : IAssistantCatalogTool
{
    public const string ToolName = "catalog.product.describe";
    public string Name => ToolName;
    public CatalogQueryResult Execute(Guid storeId, string query)
    {
        if (storeId == Guid.Empty) throw new ArgumentException("A valid store is required.", nameof(storeId));
        if (string.IsNullOrWhiteSpace(query))
            return new CatalogQueryResult(CatalogQueryResolutionStatus.NotFound, null, null, [], null);
        return queryService.Query(storeId, query);
    }
}
