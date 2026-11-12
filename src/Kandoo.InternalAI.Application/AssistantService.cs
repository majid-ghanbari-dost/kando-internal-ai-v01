using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Application;

public sealed class AssistantService(IAssistantModelProvider modelProvider, IAssistantToolRegistry toolRegistry)
{
    public async Task<AssistantResponse> AskAsync(AssistantRequest request, CancellationToken cancellationToken)
    {
        if (request.StoreId == Guid.Empty) throw new ArgumentException("A valid store is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Question)) throw new ArgumentException("A question is required.", nameof(request));

        var plan = await modelProvider.CreatePlanAsync(request.Question, cancellationToken);
        if (!toolRegistry.TryGet(plan.ToolName, out var tool) ||
            !string.Equals(tool.Name, ProductLookupTool.ToolName, StringComparison.Ordinal))
            return new AssistantResponse("برای این درخواست ابزار مجاز و پشتیبانی‌شده‌ای ندارم.", false, "fixture", []);

        var result = await tool.ExecuteAsync(request.StoreId, new ProductLookupArguments(plan.Query), cancellationToken);
        if (result.Products.Count == 0)
            return new AssistantResponse("در داده آزمایشی این فروشگاه محصول منطبقی پیدا نشد؛ برای پاسخ دقیق‌تر نام یا کد محصول را مشخص کنید.", false, "fixture", []);

        var evidence = result.Products.Select(p => new Evidence(p.SourceId, p.ProductId, p.Name, p.ObservedAtUtc)).ToArray();
        var lines = result.Products.Select(p => $"- {p.Name} | کد: {p.ProductId} | {p.Description} | واحد: {p.Unit} | قیمت آزمایشی: {p.CurrentPriceRials:N0} ریال | موجودی آزمایشی: {p.CurrentStock:N2}");
        return new AssistantResponse("نتیجه بر اساس داده آزمایشی فروشگاه (نه داده زنده):\n" + string.Join("\n", lines), true, "fixture", evidence);
    }
}
