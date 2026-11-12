using System.Globalization;
using System.Text;

namespace Kandoo.InternalAI.Application;

public static class PersianTextNormalizer
{
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var builder = new StringBuilder(value.Length);

        foreach (var original in value)
        {
            var character = original switch
            {
                'ي' or 'ى' => 'ی',
                'ك' => 'ک',
                '‌' or '‍' => ' ',
                >= '۰' and <= '۹' => (char)('0' + original - '۰'),
                >= '٠' and <= '٩' => (char)('0' + original - '٠'),
                _ => original
            };

            if (char.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark)
                continue;

            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
            else
                builder.Append(' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
