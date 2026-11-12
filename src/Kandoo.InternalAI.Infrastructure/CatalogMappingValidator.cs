using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

public sealed class CatalogMappingValidator
{
    private static readonly string[] SensitiveNameTokens =
    [
        "phone", "mobile", "email", "address", "nationalid", "national_id",
        "customer", "person", "نام مشتری", "شماره تماس", "تلفن", "موبایل",
        "ایمیل", "آدرس", "کد ملی"
    ];

    public CatalogMappingProposal CreateProposal(
        StoreCatalogProfile profile,
        string actualSchemaVersion,
        IEnumerable<SourceFieldDefinition> sourceFields,
        IReadOnlyDictionary<string, ProductAttributeDefinition> approvedFieldDefinitions)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(sourceFields);
        ArgumentNullException.ThrowIfNull(approvedFieldDefinitions);

        var drift = !string.Equals(profile.SourceSchemaVersion, actualSchemaVersion, StringComparison.Ordinal);
        var mappings = new List<FieldMappingProposal>();
        var quarantined = new List<string>();
        var excluded = new List<string>();
        var issues = new List<string>();

        foreach (var field in sourceFields.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (IsSensitiveFieldName(field.Name))
            {
                excluded.Add(field.Name);
                continue;
            }

            if (!approvedFieldDefinitions.TryGetValue(field.Name, out var definition) ||
                !definition.IsAllowlisted ||
                definition.Sensitivity != CatalogSensitivity.Public)
            {
                quarantined.Add(field.Name);
                continue;
            }

            if (!IsCompatible(field.DataType, definition.ValueType))
            {
                quarantined.Add(field.Name);
                issues.Add($"Type mismatch for source field '{field.Name}'.");
                continue;
            }

            mappings.Add(new FieldMappingProposal(
                field.Name, definition.Key, definition.ValueType, definition.Sensitivity));
        }

        if (drift)
            issues.Add($"Schema drift detected: expected '{profile.SourceSchemaVersion}', received '{actualSchemaVersion}'.");

        return new CatalogMappingProposal(
            profile.StoreId,
            profile.ProfileId,
            profile.ProfileVersion + 1,
            actualSchemaVersion,
            drift ? CatalogMappingStatus.Invalidated : CatalogMappingStatus.Proposed,
            drift,
            mappings,
            quarantined,
            excluded,
            issues);
    }

    private static bool IsSensitiveFieldName(string name)
    {
        var normalized = CatalogTextNormalizer.Normalize(name).Replace(" ", string.Empty);
        return SensitiveNameTokens.Any(token =>
            normalized.Contains(CatalogTextNormalizer.Normalize(token).Replace(" ", string.Empty), StringComparison.Ordinal));
    }

    private static bool IsCompatible(string sourceType, CatalogValueType targetType)
    {
        var source = sourceType.Trim().ToLowerInvariant();
        return targetType switch
        {
            CatalogValueType.String => source is "string" or "text" or "nvarchar" or "varchar",
            CatalogValueType.Decimal => source is "decimal" or "number" or "double" or "float",
            CatalogValueType.Integer => source is "int" or "integer" or "long" or "bigint",
            CatalogValueType.Boolean => source is "bool" or "boolean" or "bit",
            CatalogValueType.Date => source is "date" or "datetime" or "datetime2",
            _ => false
        };
    }
}
