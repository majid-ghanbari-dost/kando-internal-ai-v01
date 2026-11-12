using Kandoo.InternalAI.Core;
namespace Kandoo.InternalAI.Application;
public interface IAssistantModelProvider
{
    Task<AssistantPlan> CreatePlanAsync(string question, CancellationToken cancellationToken);
}
public interface IStoreReadGateway
{
    Task<IReadOnlyList<ProductReadModel>> FindProductsAsync(Guid storeId, string query, CancellationToken cancellationToken);
}
public interface IAssistantTool
{
    string Name { get; }
    Task<ProductLookupResult> ExecuteAsync(Guid storeId, ProductLookupArguments arguments, CancellationToken cancellationToken);
}
public interface IAssistantCatalogTool
{
    string Name { get; }
    CatalogQueryResult Execute(Guid storeId, string query);
}
public interface IAssistantToolRegistry
{
    bool TryGet(string name, out IAssistantTool tool);
}
