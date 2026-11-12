using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Application;

public sealed record ParsedProductQuery(AssistantIntent Intent, string Query);

public static class PersianProductQueryParser
{
    private static readonly HashSet<string> PriceTerms = new(StringComparer.Ordinal)
    {
        "قیمت", "بها", "چنده", "چند", "چقدر", "ارزش"
    };

    private static readonly HashSet<string> StockTerms = new(StringComparer.Ordinal)
    {
        "موجودی", "موجود", "انبار", "تعداد", "چندتا", "چندت"
    };

    private static readonly HashSet<string> FillerTerms = new(StringComparer.Ordinal)
    {
        "قیمت", "بها", "چنده", "چند", "چقدر", "ارزش",
        "موجودی", "موجود", "انبار", "تعداد", "چندتا", "چندت",
        "لطفا", "لطفاً", "رو", "را", "بگو", "بده", "بفرما",
        "نشون", "نشان", "ده", "نمایش", "پیدا", "کن", "کنید",
        "محصول", "کالا", "درباره", "اطلاعات", "مشخصات",
        "الان", "فعلی", "داریم", "دارید", "هست", "است", "برای", "از", "من"
    };

    public static ParsedProductQuery Parse(string question)
    {
        var normalized = PersianTextNormalizer.Normalize(question);
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var intent = tokens.Any(PriceTerms.Contains)
            ? AssistantIntent.CurrentPrice
            : tokens.Any(StockTerms.Contains)
                ? AssistantIntent.CurrentStock
                : AssistantIntent.ProductDetails;

        var query = string.Join(' ', tokens.Where(token => !FillerTerms.Contains(token)));
        return new ParsedProductQuery(intent, query);
    }
}
