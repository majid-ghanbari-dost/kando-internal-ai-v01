# Mapping Review Decision v1

**Contract:** `kandoo.catalog-mapping-review-decision`  
**Version:** 1  
**Status:** Fixture-only review artifact; not an authorization credential

## Purpose

This artifact records a human review outcome against one exact, previously verified Build 0.5 Mapping Proposal. It is portable, deterministic and integrity-checked. It does not authenticate a reviewer, persist an approval, activate a mapping, or authorize production use.

## Envelope

The JSON envelope has exactly three top-level properties, in this canonical order:

1. `contract`: `kandoo.catalog-mapping-review-decision`
2. `contract_version`: integer `1`
3. `decision`: the decision record

The decision record contains exactly:

- `decision_id`: deterministic `cmrd-` identifier.
- `proposal_id`: exact Build 0.5 proposal identifier.
- `proposal_content_sha256`: exact Build 0.5 proposal digest.
- `store_id`: store UUID from the verified proposal.
- `profile_id`: profile identifier from the verified proposal.
- `decision`: one of `Approved`, `Rejected`, `ChangesRequested`.
- `reviewer_ref`: opaque fixture reference, not proof of identity.
- `reviewed_at_utc`: normalized UTC timestamp in fixed seven-fractional-digit form.
- `reason`: non-empty review reason, maximum 2,000 characters.
- `decision_content_sha256`: SHA-256 over canonical decision record excluding this digest.

The enum has an explicit `Unspecified = 0` sentinel. It is rejected. Omitting a decision can never silently mean Approved.

## Determinism and verification

- UTF-8 JSON is emitted with fixed property order and compact formatting.
- Decision ID is derived from the canonical record identity excluding decision ID and digest.
- Content digest covers the canonical record including decision ID but excluding the digest itself.
- Import requires a separately verified Build 0.5 proposal and checks proposal ID, proposal digest, store ID and profile ID.
- Unknown or duplicate JSON properties, invalid enums, malformed values, tampering, non-canonical JSON and mismatched proposal/store/profile are rejected.
- An identical decision input produces the same decision ID and bytes. Different outcomes/reasons/timestamps produce distinct identities.

## Review semantics and limitations

- `Approved` means only “the fixture reviewer marked this proposal Approved” within this artifact.
- It does **not** activate or apply a mapping and is not production authorization.
- `reviewer_ref` is caller-supplied opaque metadata. No authentication or authorization is performed.
- There is no durable registry, so global duplicate-decision prevention and revocation are intentionally not claimed. Deterministic IDs make exact duplicate artifacts detectable by a future controlled registry.
- A changed proposal must be reviewed again because its proposal ID and content digest change.
- No real store data, PII values, credentials, Hub connection, external API or mapping persistence is involved.
