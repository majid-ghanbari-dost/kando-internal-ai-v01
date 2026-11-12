using Kandoo.InternalAI.Application;
using Kandoo.InternalAI.Core;
namespace Kandoo.InternalAI.Infrastructure;
public sealed class FakeModelProvider : IAssistantModelProvider
{
    private static readonly HashSet<string> CatalogAttributeMarkers = new(StringComparer.Ordinal)
    {
        "قطر", "جنس", "رنگ", "سایز", "اندازه", "وزن", "حجم", "برند", "شکل", "قالب", "پارچه"
    };
    private static readonly HashSet<string> QuestionFillers = new(StringComparer.Ordinal)
    {
        "چیست", "چیه", "است", "هست", "چه", "رو", "را", "لطفا", "لطفاً", "میشه", "میباشد"
    };
    public Task<AssistantPlan> CreatePlanAsync(string question, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Deterministic routing only; this is not a language model or semantic classifier.
        var normalized = PersianTextNormalizer.Normalize(question);
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Any(CatalogAttributeMarkers.Contains))
        {
            var query = string.Join(' ', tokens
                .Where(token => !CatalogAttributeMarkers.Contains(token))
                .Where(token => !QuestionFillers.Contains(token)));
            return Task.FromResult(new AssistantPlan(CatalogQueryTool.ToolName, query, AssistantIntent.CatalogAttributes));
        }
        var parsed = PersianProductQueryParser.Parse(question);
        return Task.FromResult(new AssistantPlan(ProductLookupTool.ToolName, parsed.Query, parsed.Intent));
    }
}
