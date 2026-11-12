using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Application;

public sealed record ParsedProductQuery(AssistantIntent Intent, string Query);

public static class PersianProductQueryParser
{
    private static readonly string[][] PriceMarkers =
    [
        ["قیمت"], ["بها"], ["چنده"], ["چند", "است"], ["چقدر"]
    ];

    private static readonly string[][] StockMarkers =
    [
        ["موجودی"], ["موجود"], ["انبار"], ["تعداد"], ["چندتا"], ["چند", "تا"], ["چندت"]
    ];

    // Only boundary phrases are removed. Words inside a possible product name are preserved.
    private static readonly string[][] PrefixPhrases =
    [
        ["مشخصات", "محصول"], ["اطلاعات", "محصول"], ["لطفا"],
        ["قیمت"], ["بها"], ["موجودی"], ["موجود"], ["انبار"], ["تعداد"],
        ["چندتا"], ["چند", "تا"], ["مشخصات"], ["اطلاعات"],
        ["محصول"], ["کالا"], ["برای"], ["از"]
    ];

    private static readonly string[][] SuffixPhrases =
    [
        ["رو", "نشان", "بده"], ["را", "نشان", "بده"], ["رو", "نشون", "بده"],
        ["را", "نشون", "بده"], ["رو", "نمایش", "بده"], ["را", "نمایش", "بده"],
        ["رو", "بگو"], ["را", "بگو"], ["رو", "بفرما"], ["را", "بفرما"],
        ["چند", "تا", "داریم"], ["چند", "تا", "موجود", "داریم"], ["داریم"],
        ["چنده"], ["چقدر"], ["چند"], ["هست"], ["است"]
    ];

    public static ParsedProductQuery Parse(string question)
    {
        ArgumentNullException.ThrowIfNull(question);

        var tokens = PersianTextNormalizer.Normalize(question)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var intent = DetectIntent(tokens);
        StripBoundaryPhrases(tokens);

        return new ParsedProductQuery(intent, string.Join(' ', tokens));
    }

    private static AssistantIntent DetectIntent(IReadOnlyList<string> tokens)
    {
        var boundary = tokens.Take(3).Concat(tokens.TakeLast(4)).ToHashSet(StringComparer.Ordinal);

        if (PriceMarkers.Any(marker => ContainsPhrase(tokens, marker)) &&
            (boundary.Contains("قیمت") || boundary.Contains("بها") ||
             boundary.Contains("چنده") || boundary.Contains("چقدر") ||
             tokens.TakeLast(2).SequenceEqual(["چند", "است"])))
            return AssistantIntent.CurrentPrice;

        if (StockMarkers.Any(marker => ContainsPhrase(tokens, marker)) &&
            (boundary.Overlaps(["موجودی", "موجود", "انبار", "تعداد", "چندتا", "چندت", "چند"]) ||
             ContainsPhrase(tokens, ["چند", "تا", "داریم"])))
            return AssistantIntent.CurrentStock;

        return AssistantIntent.ProductDetails;
    }

    private static void StripBoundaryPhrases(List<string> tokens)
    {
        var changed = true;
        while (changed && tokens.Count > 0)
        {
            changed = false;
            foreach (var phrase in PrefixPhrases.OrderByDescending(x => x.Length))
            {
                if (StartsWith(tokens, phrase))
                {
                    tokens.RemoveRange(0, phrase.Length);
                    changed = true;
                    break;
                }
            }
        }

        changed = true;
        while (changed && tokens.Count > 0)
        {
            changed = false;
            foreach (var phrase in SuffixPhrases.OrderByDescending(x => x.Length))
            {
                if (EndsWith(tokens, phrase))
                {
                    tokens.RemoveRange(tokens.Count - phrase.Length, phrase.Length);
                    changed = true;
                    break;
                }
            }
        }
    }

    private static bool ContainsPhrase(IReadOnlyList<string> tokens, IReadOnlyList<string> phrase)
    {
        if (phrase.Count > tokens.Count) return false;
        for (var start = 0; start <= tokens.Count - phrase.Count; start++)
        {
            var matches = true;
            for (var offset = 0; offset < phrase.Count; offset++)
                if (!string.Equals(tokens[start + offset], phrase[offset], StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            if (matches) return true;
        }
        return false;
    }

    private static bool StartsWith(IReadOnlyList<string> tokens, IReadOnlyList<string> phrase) =>
        phrase.Count <= tokens.Count &&
        phrase.Select((token, index) => string.Equals(tokens[index], token, StringComparison.Ordinal)).All(x => x);

    private static bool EndsWith(IReadOnlyList<string> tokens, IReadOnlyList<string> phrase) =>
        phrase.Count <= tokens.Count &&
        phrase.Select((token, index) => string.Equals(tokens[tokens.Count - phrase.Count + index], token, StringComparison.Ordinal)).All(x => x);
}
