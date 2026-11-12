using System.Text;

namespace Kandoo.InternalAI.Infrastructure;

internal static class CatalogTextNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Normalize(NormalizationForm.FormKC)
            .Replace('ي', 'ی')
            .Replace('ك', 'ک')
            .Trim()
            .ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        var previousWasSpace = false;
        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character) || character is '\u200c' or '\u200d')
            {
                if (!previousWasSpace) builder.Append(' ');
                previousWasSpace = true;
                continue;
            }
            builder.Append(character);
            previousWasSpace = false;
        }
        return builder.ToString().Trim();
    }
}
