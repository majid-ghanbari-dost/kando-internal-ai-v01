using System.Text.Json;
using Kandoo.InternalAI.Application;
using Kandoo.InternalAI.Core;
using Xunit;

namespace Kandoo.InternalAI.Tests;

public sealed class PersianProductQueryParserTests
{
    [Fact]
    public void Normalizer_UnifiesArabicLettersDigitsAndHalfSpace()
    {
        var normalized = PersianTextNormalizer.Normalize("كالا ي ۱۲‌عدد");
        Assert.Equal("کالا ی 12 عدد", normalized);
    }

    [Fact]
    public void Parser_EvaluatesVersionedSyntheticCorpusWithExactIntentAndQuery()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "persian-query-corpus.v1.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        Assert.Equal("kandoo-fa-product-query-v1", root.GetProperty("corpusId").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("synthetic", root.GetProperty("source").GetString());

        var total = 0;
        var exactIntent = 0;
        var exactQuery = 0;
        var failures = new List<string>();

        foreach (var item in root.GetProperty("cases").EnumerateArray())
        {
            total++;
            var id = item.GetProperty("id").GetString()!;
            var parsed = PersianProductQueryParser.Parse(item.GetProperty("question").GetString()!);
            var expectedIntent = Enum.Parse<AssistantIntent>(item.GetProperty("expectedIntent").GetString()!);
            var expectedQuery = item.GetProperty("expectedQuery").GetString()!;

            if (parsed.Intent == expectedIntent) exactIntent++;
            else failures.Add($"{id}: intent expected {expectedIntent}, actual {parsed.Intent}");

            if (parsed.Query == expectedQuery) exactQuery++;
            else failures.Add($"{id}: query expected '{expectedQuery}', actual '{parsed.Query}'");
        }

        Assert.True(total >= 15, $"Corpus unexpectedly small: {total}");
        Assert.True(exactIntent == total, $"Intent exact-match: {exactIntent}/{total}. {string.Join("; ", failures)}");
        Assert.True(exactQuery == total, $"Query exact-match: {exactQuery}/{total}. {string.Join("; ", failures)}");
    }

    [Fact]
    public void Parser_PreservesFillerLikeWordsInsideProductPhrase()
    {
        var parsed = PersianProductQueryParser.Parse("بادکنک لطفا قرمز");
        Assert.Equal("بادکنک لطفا قرمز", parsed.Query);
    }

    [Fact]
    public void Parser_PreservesProductTokenThatContainsTheWordSeveral()
    {
        var parsed = PersianProductQueryParser.Parse("قیمت چندتایی بادکنک لاتکس");
        Assert.Equal("چندتایی بادکنک لاتکس", parsed.Query);
        Assert.Equal(AssistantIntent.CurrentPrice, parsed.Intent);
    }

    [Fact]
    public void Parser_DoesNotInventAProductQueryWhenOnlyIntentWasProvided()
    {
        var parsed = PersianProductQueryParser.Parse("قیمت");
        Assert.Equal(string.Empty, parsed.Query);
        Assert.Equal(AssistantIntent.CurrentPrice, parsed.Intent);
    }
}
