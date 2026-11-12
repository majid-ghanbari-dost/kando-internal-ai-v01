using Kandoo.InternalAI.Application;

namespace Kandoo.InternalAI.Infrastructure;

public sealed class AssistantToolRegistry : IAssistantToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAssistantTool> _tools;
    public AssistantToolRegistry(IEnumerable<IAssistantTool> tools) =>
        _tools = tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
    public bool TryGet(string name, out IAssistantTool tool) => _tools.TryGetValue(name, out tool!);
}
