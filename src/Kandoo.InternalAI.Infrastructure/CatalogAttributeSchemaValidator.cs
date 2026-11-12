using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

public sealed class CatalogAttributeSchemaValidator
{
    public CatalogAttributeValidationResult Validate(
        CatalogProduct product,
        IReadOnlyDictionary<string, ProductAttributeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(definitions);

        var issues = new List<string>();
        foreach (var pair in product.Attributes.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var value = pair.Value;
            if (!string.Equals(pair.Key, value.Key, StringComparison.Ordinal))
                issues.Add($"Attribute dictionary key '{pair.Key}' does not match value key '{value.Key}'.");

            if (!definitions.TryGetValue(pair.Key, out var definition))
            {
                issues.Add($"Attribute '{pair.Key}' has no approved definition.");
                continue;
            }

            if (!definition.IsAllowlisted || definition.Sensitivity != CatalogSensitivity.Public)
                issues.Add($"Attribute '{pair.Key}' is not approved for Store Intelligence.");

            if (definition.ValueType != value.ValueType)
                issues.Add($"Attribute '{pair.Key}' type does not match its definition.");

            if (!HasExactlyOneMatchingPayload(value))
                issues.Add($"Attribute '{pair.Key}' must contain exactly one value matching its declared type.");

            if (value.ValueType == CatalogValueType.String &&
                definition.AllowedValues.Count > 0 &&
                (string.IsNullOrWhiteSpace(value.StringValue) ||
                 !definition.AllowedValues.Contains(value.StringValue, StringComparer.Ordinal)))
                issues.Add($"Attribute '{pair.Key}' is outside its allowed values.");
        }

        return new CatalogAttributeValidationResult(product.StoreId, product.ProductId, issues.Count == 0, issues);
    }

    private static bool HasExactlyOneMatchingPayload(CatalogAttributeValue value)
    {
        var populated = new[]
        {
            value.StringValue is not null,
            value.DecimalValue is not null,
            value.IntegerValue is not null,
            value.BooleanValue is not null,
            value.DateValue is not null
        };
        if (populated.Count(x => x) != 1) return false;

        return value.ValueType switch
        {
            CatalogValueType.String => value.StringValue is not null,
            CatalogValueType.Decimal => value.DecimalValue is not null,
            CatalogValueType.Integer => value.IntegerValue is not null,
            CatalogValueType.Boolean => value.BooleanValue is not null,
            CatalogValueType.Date => value.DateValue is not null,
            _ => false
        };
    }
}
