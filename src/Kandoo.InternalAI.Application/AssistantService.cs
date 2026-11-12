using Kandoo.InternalAI.Core;
namespace Kandoo.InternalAI.Application;
public sealed class AssistantService(
    IAssistantModelProvider modelProvider,
    IAssistantToolRegistry toolRegistry,
    IAssistantCatalogTool? catalogTool = null)
{
    public async Task<AssistantResponse> AskAsync(AssistantRequest request, CancellationToken cancellationToken)
    {
        if (request.StoreId == Guid.Empty) throw new ArgumentException("A valid store is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Question)) throw new ArgumentException("A question is required.", nameof(request));
        var plan = await modelProvider.CreatePlanAsync(request.Question, cancellationToken);
        if (string.Equals(plan.ToolName, "catalog.product.describe", StringComparison.Ordinal))
            return ExecuteCatalogQuery(request.StoreId, plan);
        if (!toolRegistry.TryGet(plan.ToolName, out var tool) ||
            !string.Equals(tool.Name, ProductLookupTool.ToolName, StringComparison.Ordinal))
            return EmptyResponse("ابزار مجاز و پشتیبانی‌شده‌ای برای این درخواست ندارم.", plan.Intent, "unsupported");
        if (string.IsNullOrWhiteSpace(plan.Query))
            return EmptyResponse("نام یا کد محصول را مشخص کنید تا بتوانم جست‌وجو کنم.", plan.Intent, "query_required");
        var result = await tool.ExecuteAsync(request.StoreId, new ProductLookupArguments(plan.Query), cancellationToken);
        if (result.Products.Count == 0)
            return EmptyResponse("در داده آزمایشی این فروشگاه محصول منطبقی پیدا نشد؛ نام یا کد محصول را دقیق‌تر وارد کنید.", plan.Intent, "not_found");
        if (result.Products.Count > 1)
        {
            var orderedMatches = result.Products.OrderBy(product => product.Name, StringComparer.Ordinal)
                .ThenBy(product => product.ProductId, StringComparer.Ordinal).ToArray();
            var candidates = orderedMatches.Select(product =>
                new ProductCandidate(product.ProductId, product.Name, product.Description, product.Unit)).ToArray();
            var evidence = orderedMatches.Select(product =>
                new Evidence(product.SourceId, product.ProductId, product.Name, product.ObservedAtUtc)).ToArray();
            var candidateLines = candidates.Select(candidate => $"- {candidate.Name} (کد {candidate.ProductId})");
            var answer = "چند محصول با این عبارت مطابقت دارند و هنوز محصولی انتخاب نشده است:\n" +
                         string.Join("\n", candidateLines) + "\nلطفاً نام کامل یا کد محصول موردنظر را بفرستید.";
            return new AssistantResponse(answer, true, "fixture", plan.Intent.ToString(), [], evidence,
                "clarification_required", "کدام محصول را می‌خواهید؟ نام کامل یا کد محصول را مشخص کنید.", candidates);
        }
        var products = result.Products.Select(p => new ProductAnswer(
            p.ProductId, p.Name, p.Description, p.Unit, p.CurrentPriceRials,
            p.CurrentStock, p.ObservedAtUtc, p.SourceId)).ToArray();
        var resolvedEvidence = products.Select(p => new Evidence(p.SourceId, p.ProductId, p.Name, p.ObservedAtUtc)).ToArray();
        var lines = products.Select(product => FormatProduct(product, plan.Intent));
        var resolvedAnswer = $"نتیجه بر اساس داده آزمایشی فروشگاه (نه داده زنده) — نوع درخواست: {IntentLabel(plan.Intent)}\n" +
                             string.Join("\n", lines);
        return new AssistantResponse(resolvedAnswer, true, "fixture", plan.Intent.ToString(), products, resolvedEvidence);
    }
    private AssistantResponse ExecuteCatalogQuery(Guid storeId, AssistantPlan plan)
    {
        if (catalogTool is null || !string.Equals(catalogTool.Name, "catalog.product.describe", StringComparison.Ordinal))
            return EmptyResponse("ابزار مجاز کاتالوگ در این میزبان ثبت نشده است.", plan.Intent, "unsupported");
        if (string.IsNullOrWhiteSpace(plan.Query))
            return EmptyResponse("نام یا کد محصول را برای بررسی ویژگی‌ها مشخص کنید.", plan.Intent, "query_required");
        var result = catalogTool.Execute(storeId, plan.Query);
        if (result.Status == CatalogQueryResolutionStatus.NotFound)
            return EmptyResponse("در کاتالوگ آزمایشی این فروشگاه محصول منطبقی پیدا نشد.", plan.Intent, "not_found");
        if (result.Status == CatalogQueryResolutionStatus.ClarificationRequired)
        {
            var candidates = result.Candidates.OrderBy(item => item.Name, StringComparer.Ordinal)
                .ThenBy(item => item.ProductId, StringComparer.Ordinal)
                .Select(item => new ProductCandidate(item.ProductId, item.Name, item.CategoryPath, item.Unit)).ToArray();
            var lines = candidates.Select(item => $"- {item.Name} (کد {item.ProductId})");
            var question = result.ClarificationQuestion ?? "لطفاً نام کامل یا کد محصول را مشخص کنید.";
            return new AssistantResponse(
                "چند محصول در کاتالوگ آزمایشی مطابقت دارند و هنوز محصولی انتخاب نشده است:\n" +
                string.Join("\n", lines) + "\n" + question,
                true, "fixture", plan.Intent.ToString(), [], [], "clarification_required", question, candidates);
        }
        if (result.Product is null || result.Evidence is null)
            return EmptyResponse("نتیجه کاتالوگ ناقص است و نمی‌توان آن را تأیید کرد.", plan.Intent, "unsupported");
        var product = result.Product;
        var attributes = product.Attributes.Values.OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => new CatalogAttributeAnswer(
                value.Key, AttributeLabel(value.Key), value.ValueType, value.StringValue,
                value.DecimalValue, value.IntegerValue, value.BooleanValue, value.DateValue, value.SourceField)).ToArray();
        var catalogAnswer = new CatalogProductAnswer(product.ProductId, product.Name, product.CategoryPath,
            product.Unit, attributes, result.Evidence.ProfileId, result.Evidence.SourceVersion);
        var observedAt = attributes.Select(attribute => product.Attributes[attribute.Key].ObservedAtUtc)
            .DefaultIfEmpty(DateTimeOffset.UnixEpoch).Max();
        var evidence = new Evidence(result.Evidence.SourceId, product.ProductId, product.Name, observedAt);
        var answer = $"مشخصات محصول بر اساس کاتالوگ آزمایشی فروشگاه — {product.Name} (کد {product.ProductId})\n" +
                     string.Join("\n", attributes.Select(FormatAttribute));
        return new AssistantResponse(answer, true, "fixture", plan.Intent.ToString(), [], [evidence],
            "resolved", null, null, catalogAnswer);
    }
    private static string FormatAttribute(CatalogAttributeAnswer attribute)
    {
        var value = attribute.ValueType switch
        {
            CatalogValueType.String => attribute.StringValue ?? "—",
            CatalogValueType.Decimal => attribute.DecimalValue?.ToString("0.##") ?? "—",
            CatalogValueType.Integer => attribute.IntegerValue?.ToString() ?? "—",
            CatalogValueType.Boolean => attribute.BooleanValue is true ? "بله" : attribute.BooleanValue is false ? "خیر" : "—",
            CatalogValueType.Date => attribute.DateValue?.ToString("yyyy-MM-dd") ?? "—",
            _ => "—"
        };
        return $"- {attribute.DisplayName}: {value}";
    }
    private static string AttributeLabel(string key) => key switch
    {
        "material" => "جنس", "diameter_cm" => "قطر (سانتی‌متر)", "color" => "رنگ",
        "shape" => "شکل", "brand" => "برند", "weight_kg" => "وزن (کیلوگرم)",
        "organic" => "ارگانیک", "volume_ml" => "حجم (میلی‌لیتر)", "size" => "سایز",
        "fabric" => "جنس پارچه", "fit" => "قالب", "waist_size" => "سایز کمر",
        _ => key
    };
    private static AssistantResponse EmptyResponse(string answer, AssistantIntent intent, string status) =>
        new(answer, false, "fixture", intent.ToString(), [], [], status);
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
