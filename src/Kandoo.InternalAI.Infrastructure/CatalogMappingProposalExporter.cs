using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

/// <summary>
/// Produces and verifies deterministic, versioned mapping-proposal review artifacts.
/// This class never applies or approves a mapping.
/// </summary>
public sealed class CatalogMappingProposalExporter
{
    public const string ContractName = "kandoo.catalog-mapping-proposal";
    public const int ContractVersion = 1;

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        SkipValidation = false
    };

    public string Export(CatalogMappingProposal proposal)
    {
        ValidateProposal(proposal);

        var proposalBytes = WriteProposalBytes(proposal);
        var proposalId = "cmp-" + Sha256(proposalBytes)[..24];
        var unsignedEnvelope = WriteEnvelopeBytes(proposalId, proposal, contentSha256: null);
        var digest = Sha256(unsignedEnvelope);
        var finalEnvelope = WriteEnvelopeBytes(proposalId, proposal, digest);
        return Encoding.UTF8.GetString(finalEnvelope);
    }

    /// <summary>
    /// Imports only the exact canonical v1 representation emitted by Export.
    /// Unknown/duplicate properties, non-canonical ordering, and digest tampering are rejected.
    /// </summary>
    public CatalogMappingProposal ImportAndVerify(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });

            var root = document.RootElement;
            EnsureProperties(root, "contract", "contract_version", "proposal_id", "proposal", "content_sha256");

            if (ReadString(root, "contract") != ContractName)
                throw new InvalidDataException("Unsupported mapping proposal contract.");

            if (ReadInt32(root, "contract_version") != ContractVersion)
                throw new InvalidDataException("Unsupported mapping proposal contract version.");

            var proposal = ReadProposal(root.GetProperty("proposal"));
            var canonical = Export(proposal);
            if (!string.Equals(canonical, json, StringComparison.Ordinal))
                throw new InvalidDataException("Mapping proposal digest or canonical representation is invalid.");

            return proposal;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Mapping proposal JSON is malformed.", exception);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("Mapping proposal contains an invalid encoded value.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidDataException("Mapping proposal contains a value of the wrong JSON type.", exception);
        }
    }

    private static void ValidateProposal(CatalogMappingProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        if (proposal.StoreId == Guid.Empty ||
            string.IsNullOrWhiteSpace(proposal.ProfileId) ||
            proposal.ProposedProfileVersion < 1 ||
            string.IsNullOrWhiteSpace(proposal.SourceSchemaVersion) ||
            !Enum.IsDefined(proposal.Status))
            throw new InvalidDataException("Mapping proposal identity, version, schema, or status is invalid.");

        ArgumentNullException.ThrowIfNull(proposal.Mappings);
        ArgumentNullException.ThrowIfNull(proposal.QuarantinedFields);
        ArgumentNullException.ThrowIfNull(proposal.ExcludedSensitiveFields);
        ArgumentNullException.ThrowIfNull(proposal.ValidationIssues);

        if (proposal.Mappings.Any(x =>
                x is null ||
                string.IsNullOrWhiteSpace(x.SourceField) ||
                string.IsNullOrWhiteSpace(x.TargetAttributeKey) ||
                !Enum.IsDefined(x.TargetValueType) ||
                x.Sensitivity != CatalogSensitivity.Public))
            throw new InvalidDataException("Only complete, public, typed mapping candidates can be exported.");

        if (proposal.Mappings.GroupBy(x => x.SourceField, StringComparer.Ordinal).Any(x => x.Count() > 1))
            throw new InvalidDataException("A source field cannot have multiple mapping candidates.");

        if (proposal.QuarantinedFields.Any(string.IsNullOrWhiteSpace) ||
            proposal.ExcludedSensitiveFields.Any(string.IsNullOrWhiteSpace) ||
            proposal.ValidationIssues.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Proposal field names and validation issues cannot be empty.");

        if (proposal.QuarantinedFields.Distinct(StringComparer.Ordinal).Count() != proposal.QuarantinedFields.Count ||
            proposal.ExcludedSensitiveFields.Distinct(StringComparer.Ordinal).Count() != proposal.ExcludedSensitiveFields.Count)
            throw new InvalidDataException("Proposal field lists cannot contain duplicate entries.");
    }

    private static CatalogMappingProposal ReadProposal(JsonElement element)
    {
        EnsureProperties(element, "store_id", "profile_id", "proposed_profile_version",
            "source_schema_version", "status", "has_schema_drift", "mappings",
            "quarantined_fields", "excluded_sensitive_fields", "validation_issues");

        var mappingsElement = element.GetProperty("mappings");
        if (mappingsElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Mappings must be an array.");

        var mappings = mappingsElement.EnumerateArray().Select(item =>
        {
            EnsureProperties(item, "source_field", "target_attribute_key", "target_value_type", "sensitivity");
            return new FieldMappingProposal(
                ReadString(item, "source_field"),
                ReadString(item, "target_attribute_key"),
                ReadEnum<CatalogValueType>(item, "target_value_type"),
                ReadEnum<CatalogSensitivity>(item, "sensitivity"));
        }).ToArray();

        return new CatalogMappingProposal(
            Guid.Parse(ReadString(element, "store_id")),
            ReadString(element, "profile_id"),
            ReadInt32(element, "proposed_profile_version"),
            ReadString(element, "source_schema_version"),
            ReadEnum<CatalogMappingStatus>(element, "status"),
            ReadBoolean(element, "has_schema_drift"),
            mappings,
            ReadStringArray(element, "quarantined_fields"),
            ReadStringArray(element, "excluded_sensitive_fields"),
            ReadStringArray(element, "validation_issues"));
    }

    private static void EnsureProperties(JsonElement element, params string[] expected)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Expected a JSON object.");

        var actual = new List<string>();
        foreach (var property in element.EnumerateObject())
        {
            if (actual.Contains(property.Name, StringComparer.Ordinal))
                throw new InvalidDataException($"Duplicate JSON property '{property.Name}'.");
            actual.Add(property.Name);
        }

        if (!actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw new InvalidDataException("JSON contains missing or unsupported properties.");
    }

    private static string ReadString(JsonElement element, string name) =>
        element.GetProperty(name).GetString()
        ?? throw new InvalidDataException($"Property '{name}' cannot be null.");

    private static int ReadInt32(JsonElement element, string name) => element.GetProperty(name).GetInt32();
    private static bool ReadBoolean(JsonElement element, string name) => element.GetProperty(name).GetBoolean();

    private static T ReadEnum<T>(JsonElement element, string name) where T : struct, Enum
    {
        var value = ReadString(element, name);
        if (!Enum.TryParse<T>(value, ignoreCase: false, out var parsed) || !Enum.IsDefined(parsed))
            throw new InvalidDataException($"Property '{name}' contains an unsupported enum value.");
        return parsed;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string name)
    {
        var array = element.GetProperty(name);
        if (array.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"Property '{name}' must be an array.");
        return array.EnumerateArray().Select(x =>
            x.GetString() ?? throw new InvalidDataException($"Property '{name}' cannot contain null.")).ToArray();
    }

    private static byte[] WriteProposalBytes(CatalogMappingProposal proposal)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
            WriteProposal(writer, proposal);
        return stream.ToArray();
    }

    private static byte[] WriteEnvelopeBytes(string proposalId, CatalogMappingProposal proposal, string? contentSha256)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
        {
            writer.WriteStartObject();
            writer.WriteString("contract", ContractName);
            writer.WriteNumber("contract_version", ContractVersion);
            writer.WriteString("proposal_id", proposalId);
            writer.WritePropertyName("proposal");
            WriteProposal(writer, proposal);
            if (contentSha256 is not null)
                writer.WriteString("content_sha256", contentSha256);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void WriteProposal(Utf8JsonWriter writer, CatalogMappingProposal proposal)
    {
        writer.WriteStartObject();
        writer.WriteString("store_id", proposal.StoreId.ToString("D"));
        writer.WriteString("profile_id", proposal.ProfileId);
        writer.WriteNumber("proposed_profile_version", proposal.ProposedProfileVersion);
        writer.WriteString("source_schema_version", proposal.SourceSchemaVersion);
        writer.WriteString("status", proposal.Status.ToString());
        writer.WriteBoolean("has_schema_drift", proposal.HasSchemaDrift);

        writer.WritePropertyName("mappings");
        writer.WriteStartArray();
        foreach (var mapping in proposal.Mappings
                     .OrderBy(x => x.SourceField, StringComparer.Ordinal)
                     .ThenBy(x => x.TargetAttributeKey, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("source_field", mapping.SourceField);
            writer.WriteString("target_attribute_key", mapping.TargetAttributeKey);
            writer.WriteString("target_value_type", mapping.TargetValueType.ToString());
            writer.WriteString("sensitivity", mapping.Sensitivity.ToString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        WriteSortedArray(writer, "quarantined_fields", proposal.QuarantinedFields);
        WriteSortedArray(writer, "excluded_sensitive_fields", proposal.ExcludedSensitiveFields);
        WriteSortedArray(writer, "validation_issues", proposal.ValidationIssues);
        writer.WriteEndObject();
    }

    private static void WriteSortedArray(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values.OrderBy(x => x, StringComparer.Ordinal))
            writer.WriteStringValue(value);
        writer.WriteEndArray();
    }

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
