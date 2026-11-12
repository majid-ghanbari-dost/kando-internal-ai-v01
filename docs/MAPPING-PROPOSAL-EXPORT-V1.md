# Mapping Proposal Export v1 — Human Review Format

**Status:** Implemented in Build 0.5; fixture-only.  
**Contract:** `kandoo.catalog-mapping-proposal`  
**Contract version:** `1`

## Purpose and authority

This artifact is a portable, reviewable **proposal**, not an approved mapping. Importing or verifying it does not approve, persist, activate, or apply any mapping. Human approval and any later integration are separate governance decisions.

## Envelope

The compact UTF-8 JSON envelope contains, in this exact order:

1. `contract`: `kandoo.catalog-mapping-proposal`
2. `contract_version`: integer `1`
3. `proposal_id`: deterministic `cmp-` identifier derived from the first 24 lowercase hexadecimal characters of SHA-256 over the canonical proposal object.
4. `proposal`: the typed proposal object.
5. `content_sha256`: lowercase SHA-256 over the canonical envelope containing fields 1–4 and excluding this digest field.

No export timestamp is included: identical proposal content must produce byte-identical output. All JSON is compact, UTF-8, and uses invariant JSON number/string encodings.

## Proposal object

The proposal object preserves store/profile identity, proposed profile version, source schema version, status, schema-drift flag, typed mapping candidates, quarantined field names, excluded sensitive field names, and validation issues.

Mapping entries contain only source field name, canonical target attribute key, target value type, and sensitivity. The exporter accepts only Public mapping candidates. Mapping entries are sorted by source field then target key using ordinal comparison. Quarantine, exclusion, and issue arrays are also sorted ordinally.

## Privacy and scope

The artifact contains metadata only. It never serializes source rows, product attribute values, customer/staff values, credentials, connection strings, raw schema payloads, or arbitrary source objects. Sensitive field **names** may appear in the exclusion list so a reviewer can understand why they were omitted; their values are never exported. Unknown fields remain quarantined.

The exporter does not approve mappings and has no database persistence or Hub adapter. The Build 0.5 examples are synthetic fixtures only.

## Verification and import

`CatalogMappingProposalExporter.ImportAndVerify`:
- parses the envelope and requires the exact v1 property set;
- rejects duplicate, missing, and unknown properties;
- reconstructs the typed proposal;
- re-exports it canonically, recomputing the deterministic proposal ID and SHA-256 digest;
- accepts only an exact byte-for-byte canonical representation.

Consequently, modified content, digest tampering, reordered/non-canonical JSON, unsupported versions, and unknown fields are rejected. A verified artifact is still only a proposal.

## Synthetic examples

- Party supplies: `tests/Kandoo.InternalAI.Tests/Fixtures/catalog-mapping-proposal-party.v1.json`
- Grocery: `tests/Kandoo.InternalAI.Tests/Fixtures/catalog-mapping-proposal-grocery.v1.json`
- Clothing: `tests/Kandoo.InternalAI.Tests/Fixtures/catalog-mapping-proposal-clothing.v1.json`

Each example can be imported and verified using the same contract implementation.

## Compatibility

Version 1 is intentionally strict. A future incompatible contract requires a new contract version and explicit migration/compatibility tests; silently accepting unknown properties is not supported.
