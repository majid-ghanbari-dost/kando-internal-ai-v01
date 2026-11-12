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
    public void Parser_ExtractsPriceIntentAndProductName()
    {
        var parsed = PersianProductQueryParser.Parse("قیمت بادكنک لاتكس قرمز چنده؟");
        Assert.Equal(AssistantIntent.CurrentPrice, parsed.Intent);
        Assert.Equal("بادکنک لاتکس قرمز", parsed.Query);
    }

    [Fact]
    public void Parser_ExtractsStockIntentAndProductName()
    {
        var parsed = PersianProductQueryParser.Parse("موجودی بادکنک لاتکس قرمز رو بگو");
        Assert.Equal(AssistantIntent.CurrentStock, parsed.Intent);
        Assert.Equal("بادکنک لاتکس قرمز", parsed.Query);
    }

    [Fact]
    public void Parser_PreservesProductNameForDetailsQuestion()
    {
        var parsed = PersianProductQueryParser.Parse("مشخصات محصول بادکنک لاتکس آبی را نشان بده");
        Assert.Equal(AssistantIntent.ProductDetails, parsed.Intent);
        Assert.Equal("بادکنک لاتکس آبی", parsed.Query);
    }
}
