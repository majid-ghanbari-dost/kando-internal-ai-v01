using Kandoo.InternalAI.Application;
using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

public sealed class FakeModelProvider : IAssistantModelProvider
{
    public Task<AssistantPlan> CreatePlanAsync(string question, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Deterministic rule-based baseline for Build 0.2; this is not a language model.
        var parsed = PersianProductQueryParser.Parse(question);
        return Task.FromResult(new AssistantPlan(ProductLookupTool.ToolName, parsed.Query, parsed.Intent));
    }
}
