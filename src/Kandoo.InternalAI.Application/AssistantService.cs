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
            return EmptyResponse("ابزار مجاز و پشتیبانی‌شده‌ای برای این درخواست ندارم.", plan.Intent);

        if (string.IsNullOrWhiteSpace(plan.Query))
            return EmptyResponse("نام یا کد محصول را مشخص کنید تا بتوانم جست‌وجو کنم.", plan.Intent);

        var result = await tool.ExecuteAsync(request.StoreId, new ProductLookupArguments(plan.Query), cancellationToken);
        if (result.Products.Count == 0)
            return EmptyResponse("در داده آزمایشی این فروشگاه محصول منطبقی پیدا نشد؛ نام یا کد محصول را دقیق‌تر وارد کنید.", plan.Intent);

        var products = result.Products.Select(p => new ProductAnswer(
            p.ProductId, p.Name, p.Description, p.Unit, p.CurrentPriceRials,
            p.CurrentStock, p.ObservedAtUtc, p.SourceId)).ToArray();
        var evidence = products.Select(p => new Evidence(p.SourceId, p.ProductId, p.Name, p.ObservedAtUtc)).ToArray();

        var lines = products.Select(product => FormatProduct(product, plan.Intent));
        var answer = $"نتیجه بر اساس داده آزمایشی فروشگاه (نه داده زنده) — نوع درخواست: {IntentLabel(plan.Intent)}\n" +
                     string.Join("\n", lines);

        return new AssistantResponse(answer, true, "fixture", plan.Intent.ToString(), products, evidence);
    }

    private static AssistantResponse EmptyResponse(string answer, AssistantIntent intent) =>
        new(answer, false, "fixture", intent.ToString(), [], []);

    private static string FormatProduct(ProductAnswer product, AssistantIntent intent)
    {
        var identity = $"{product.Name} (کد {product.ProductId})";
        return intent switch
        {
            AssistantIntent.CurrentPrice => $"- {identity}: {product.CurrentPriceRials:N0} ریال",
            AssistantIntent.CurrentStock => $"- {identity}: {product.CurrentStock:N2} {product.Unit}",
            _ => $"- {identity} | {product.Description} | واحد: {product.Unit} | " +
                 $"قیمت آزمایشی: {product.CurrentPriceRials:N0} ریال | موجودی آزمایشی: {product.CurrentStock:N2}"
        };
    }

    private static string IntentLabel(AssistantIntent intent) => intent switch
    {
        AssistantIntent.CurrentPrice => "قیمت",
        AssistantIntent.CurrentStock => "موجودی",
        _ => "مشخصات محصول"
    };
}
