using Kandoo.InternalAI.Application;
using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

public sealed class FakeModelProvider : IAssistantModelProvider
{
    public Task<AssistantPlan> CreatePlanAsync(string question, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Deterministic adapter for contract tests. No real model has been selected.
        return Task.FromResult(new AssistantPlan(ProductLookupTool.ToolName, question.Trim()));
    }
}
