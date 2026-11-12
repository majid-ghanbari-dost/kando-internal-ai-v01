# Build 0.5 — Versioned Mapping Proposal Export & Human Review Package

**Status:** APPROVED — fixture-only implementation in progress  
**Date:** 2026-11-12  
**Scope:** Independent Kandoo Internal AI repository; synthetic fixtures only

## 1. Problem

Build 0.3 can create a typed CatalogMappingProposal in memory. It can identify allowlisted mappings, quarantine unknown fields, exclude PII-like fields and invalidate proposals on schema drift. However, the proposal cannot yet be exported as a stable, versioned artifact for review outside the running process.

This is the remaining material gap behind the Build 0.3 ~90% completion estimate.

## 2. Goal

Create a deterministic, versioned, integrity-checkable JSON representation of a mapping proposal that a human reviewer can inspect and archive.

The export is a proposal only. It is not an approval, migration, mapping activation or instruction to connect to a database.

## 3. Proposed export contract

Introduce a versioned envelope, for example:

- contract: stable identifier such as kandoo.catalog-mapping-proposal
- contract_version: integer schema version
- proposal_id: stable identifier derived from canonical proposal content, or an explicitly documented generated identifier
- exported_at_utc: optional metadata; if included, excluded from the canonical content digest
- proposal: the existing typed CatalogMappingProposal
- content_sha256: SHA-256 of canonical UTF-8 JSON content excluding the digest field itself

Canonicalization requirements:
- UTF-8 JSON, no locale-dependent formatting.
- Stable property names and deterministic property ordering.
- Mapping, quarantine, exclusion and issue arrays sorted deterministically.
- Decimal and enum representations explicitly documented.
- No source row values, customer data, credentials, connection strings or arbitrary schema payloads.
- Export must preserve StoreId, ProfileId, proposed version, source schema version, status, drift flag, mapping targets/types/sensitivity, quarantined fields, excluded sensitive field names and validation issues.

The exact envelope and canonicalization strategy must be settled in implementation review before code is written.

## 4. Human review requirements

The artifact must make it straightforward to answer:
- Which store/profile does this proposal concern?
- Which source schema version was observed?
- Which fields are proposed for mapping and to what canonical key/type?
- Which fields are quarantined and why?
- Which fields were excluded as sensitive?
- Was schema drift detected?
- Is the proposal Proposed or Invalidated?

No approval controls or state transitions are introduced in this build.

## 5. Scope allowed after approval

- Add a strongly typed export envelope and serializer using built-in .NET libraries.
- Add deterministic canonicalization and SHA-256 integrity digest.
- Add export/import validation or round-trip parsing for the versioned contract.
- Generate synthetic export examples for all three fixture profiles.
- Add tests for deterministic output, stable ordering, round-trip, digest verification, schema drift, PII exclusion and absence of source row values.
- Add documentation describing the format and human review procedure.
- Include exported examples in CI artifacts and source archive only if they contain synthetic metadata and pass the same manifest/hash checks.

## 6. Explicit non-goals / prohibitions

- No real store database, schema discovery, connection string or production credential.
- No parent Kandoo repository changes, Hub connection, migration or live adapter.
- No automatic approval, activation or application of mappings.
- No mapping UI, database persistence or user account system.
- No LLM or external API use.
- No source row values, customer/staff PII values or secrets in the exported package.
- No PR merge without Product Owner decision.

## 7. Acceptance criteria

1. The exported contract is explicitly versioned and documented.
2. Identical proposal content produces byte-identical canonical JSON and the same content digest.
3. Reordered source collections produce the same canonical output.
4. The digest is verified before accepting an imported artifact.
5. Round-trip preserves all typed proposal fields and status.
6. Schema drift remains Invalidated after export/import.
7. Unknown fields remain quarantined and sensitive fields remain excluded.
8. No source row values, PII values, credentials or arbitrary payloads are serialized.
9. Three synthetic store profiles produce reviewable export examples.
10. CI, source archive, manifest and SHA-256 verification pass.

## 8. Approval and implementation boundary

On 12 November 2026, the Product Owner delegated the technical decision and instructed the Technical Manager to choose the best technical course. The Technical Manager approved this exact fixture-only scope and began implementation on branch `build/0.5-mapping-proposal-export`.

This approval does not authorize Hub integration, real data access, parent-repository changes, mapping activation, production persistence, or PR merge. Those remain separately gated.
