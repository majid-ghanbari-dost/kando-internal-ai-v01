using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

/// <summary>
/// Creates and verifies fixture-only human review records for an exact Build 0.5 proposal.
/// It does not authenticate reviewers, persist decisions, approve mappings, or activate them.
/// </summary>
public sealed class CatalogMappingReviewDecisionExporter
{
    public const string ContractName = "kandoo.catalog-mapping-review-decision";
    public const int ContractVersion = 1;

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        SkipValidation = false
    };

    private readonly CatalogMappingProposalExporter _proposalExporter;

    public CatalogMappingReviewDecisionExporter(CatalogMappingProposalExporter? proposalExporter = null)
    {
        _proposalExporter = proposalExporter ?? new CatalogMappingProposalExporter();
    }

    public string Export(
        string verifiedProposalJson,
        CatalogMappingReviewDecision decision,
        string reviewerRef,
        DateTimeOffset reviewedAtUtc,
        string reason)
    {
        var proposal = _proposalExporter.ImportAndVerify(verifiedProposalJson);
        ValidateDecisionInputs(decision, reviewerRef, reviewedAtUtc, reason);

        using var proposalDocument = JsonDocument.Parse(verifiedProposalJson);
        var proposalRoot = proposalDocument.RootElement;
        var proposalId = proposalRoot.GetProperty("proposal_id").GetString()
            ?? throw new InvalidDataException("Verified proposal has no proposal ID.");
        var proposalDigest = proposalRoot.GetProperty("content_sha256").GetString()
            ?? throw new InvalidDataException("Verified proposal has no content digest.");

        var unsignedRecord = new CatalogMappingReviewDecisionRecord(
            DecisionId: string.Empty,
            ProposalId: proposalId,
            ProposalContentSha256: proposalDigest,
            StoreId: proposal.StoreId,
            ProfileId: proposal.ProfileId,
            Decision: decision,
            ReviewerRef: reviewerRef,
            ReviewedAtUtc: reviewedAtUtc,
            Reason: reason,
            DecisionContentSha256: string.Empty);

        var identityBytes = WriteRecordBytes(unsignedRecord, includeDecisionId: false, includeDigest: false);
        var decisionId = "cmrd-" + Sha256(identityBytes)[..24];
        var withId = unsignedRecord with { DecisionId = decisionId };
        var digest = Sha256(WriteRecordBytes(withId, includeDecisionId: true, includeDigest: false));
        var finalRecord = withId with { DecisionContentSha256 = digest };

        return Encoding.UTF8.GetString(WriteEnvelopeBytes(finalRecord));
    }

    /// <summary>
    /// Verifies the decision envelope, its integrity digest, and exact binding to a separately
    /// verified proposal. A decision cannot be reused for a changed proposal or another store/profile.
    /// </summary>
    public CatalogMappingReviewDecisionRecord ImportAndVerify(string decisionJson, string verifiedProposalJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionJson);
        var proposal = _proposalExporter.ImportAndVerify(verifiedProposalJson);

        try
        {
            using var document = JsonDocument.Parse(decisionJson, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
            var root = document.RootElement;
            EnsureProperties(root, "contract", "contract_version", "decision");
            if (ReadString(root, "contract") != ContractName ||
                root.GetProperty("contract_version").GetInt32() != ContractVersion)
                throw new InvalidDataException("Unsupported mapping review decision contract.");

            var element = root.GetProperty("decision");
            EnsureProperties(element, "decision_id", "proposal_id", "proposal_content_sha256",
                "store_id", "profile_id", "decision", "reviewer_ref", "reviewed_at_utc",
                "reason", "decision_content_sha256");

            var record = new CatalogMappingReviewDecisionRecord(
                ReadString(element, "decision_id"),
                ReadString(element, "proposal_id"),
                ReadString(element, "proposal_content_sha256"),
                Guid.Parse(ReadString(element, "store_id")),
                ReadString(element, "profile_id"),
                ReadDecision(element),
                ReadString(element, "reviewer_ref"),
                DateTimeOffset.Parse(ReadString(element, "reviewed_at_utc"), System.Globalization.CultureInfo.InvariantCulture),
                ReadString(element, "reason"),
                ReadString(element, "decision_content_sha256"));

            ValidateDecisionInputs(record.Decision, record.ReviewerRef, record.ReviewedAtUtc, record.Reason);

            using var proposalDocument = JsonDocument.Parse(verifiedProposalJson);
            var proposalRoot = proposalDocument.RootElement;
            var expectedProposalId = ReadString(proposalRoot, "proposal_id");
            var expectedProposalDigest = ReadString(proposalRoot, "content_sha256");

            if (record.ProposalId != expectedProposalId ||
                record.ProposalContentSha256 != expectedProposalDigest ||
                record.StoreId != proposal.StoreId ||
                record.ProfileId != proposal.ProfileId)
                throw new InvalidDataException("Review decision does not match the exact proposal, store, or profile.");

            var unsignedRecord = record with { DecisionContentSha256 = string.Empty };
            var expectedDigest = Sha256(WriteRecordBytes(unsignedRecord, includeDecisionId: true, includeDigest: false));
            if (!FixedTimeEqualsHex(expectedDigest, record.DecisionContentSha256))
                throw new InvalidDataException("Review decision content digest is invalid.");

            var expectedId = "cmrd-" + Sha256(WriteRecordBytes(
                unsignedRecord with { DecisionId = string.Empty },
                includeDecisionId: false,
                includeDigest: false))[..24];
            if (!string.Equals(expectedId, record.DecisionId, StringComparison.Ordinal))
                throw new InvalidDataException("Review decision ID is invalid.");

            var canonical = Encoding.UTF8.GetString(WriteEnvelopeBytes(record));
            if (!string.Equals(canonical, decisionJson, StringComparison.Ordinal))
                throw new InvalidDataException("Review decision JSON is not canonical.");

            return record;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Review decision JSON is malformed.", exception);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("Review decision contains an invalid encoded value.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidDataException("Review decision contains a value of the wrong JSON type.", exception);
        }
    }

    private static void ValidateDecisionInputs(
        CatalogMappingReviewDecision decision,
        string reviewerRef,
        DateTimeOffset reviewedAtUtc,
        string reason)
    {
        if (!Enum.IsDefined(decision))
            throw new InvalidDataException("An explicit supported review decision is required.");
        if (string.IsNullOrWhiteSpace(reviewerRef) || reviewerRef.Length > 128)
            throw new InvalidDataException("Reviewer reference must contain 1–128 characters.");
        if (reviewedAtUtc.Offset != TimeSpan.Zero)
            throw new InvalidDataException("Review timestamp must be normalized to UTC (offset +00:00).");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 2000)
            throw new InvalidDataException("Review reason must contain 1–2000 characters.");
    }

    private static CatalogMappingReviewDecision ReadDecision(JsonElement element)
    {
        var value = ReadString(element, "decision");
        if (!Enum.TryParse<CatalogMappingReviewDecision>(value, ignoreCase: false, out var decision) ||
            !Enum.IsDefined(decision))
            throw new InvalidDataException("Review decision must be Approved, Rejected, or ChangesRequested.");
        return decision;
    }

    private static byte[] WriteEnvelopeBytes(CatalogMappingReviewDecisionRecord record)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
        {
            writer.WriteStartObject();
            writer.WriteString("contract", ContractName);
            writer.WriteNumber("contract_version", ContractVersion);
            writer.WritePropertyName("decision");
            WriteRecord(writer, record);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static byte[] WriteRecordBytes(
        CatalogMappingReviewDecisionRecord record,
        bool includeDecisionId,
        bool includeDigest)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
            WriteRecord(writer, record, includeDecisionId, includeDigest);
        return stream.ToArray();
    }

    private static void WriteRecord(
        Utf8JsonWriter writer,
        CatalogMappingReviewDecisionRecord record,
        bool includeDecisionId = true,
        bool includeDigest = true)
    {
        writer.WriteStartObject();
        if (includeDecisionId) writer.WriteString("decision_id", record.DecisionId);
        writer.WriteString("proposal_id", record.ProposalId);
        writer.WriteString("proposal_content_sha256", record.ProposalContentSha256);
        writer.WriteString("store_id", record.StoreId.ToString("D"));
        writer.WriteString("profile_id", record.ProfileId);
        writer.WriteString("decision", record.Decision.ToString());
        writer.WriteString("reviewer_ref", record.ReviewerRef);
        writer.WriteString("reviewed_at_utc", record.ReviewedAtUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteString("reason", record.Reason);
        if (includeDigest) writer.WriteString("decision_content_sha256", record.DecisionContentSha256);
        writer.WriteEndObject();
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

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static bool FixedTimeEqualsHex(string expected, string actual)
    {
        if (expected.Length != actual.Length) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected), Convert.FromHexString(actual));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
