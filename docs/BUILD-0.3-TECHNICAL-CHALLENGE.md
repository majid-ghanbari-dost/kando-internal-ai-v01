# Build 0.3 — Store Catalog Contract & Dynamic Store Attributes
Status: Product Owner approved for fixture-only implementation on 2026-11-12; Hub integration remains separately blocked
Date: 2026-11-12
Owner: Product Owner / Kandoo Project Manager (for Hub boundary)

## 1. Problem to solve
The assistant must adapt to different store catalogs without training a separate language model for every store. A party-supply store, grocery store, and clothing store may have different product attributes, units, category structures, and local terms. The AI core must not assume one fixed product schema for every business.

## 2. Proposed boundary
Separate the catalog into three layers:

1. **Canonical identity fields** — store-scoped product ID, display name, normalized name, category, unit, active state, and source lineage.
2. **Extensible descriptive attributes** — typed key/value attributes with stable namespaced keys, labels, and source provenance. Examples: balloon material/diameter/color; grocery brand/weight/expiry class; clothing size/fabric/fit.
3. **Live operational values** — current price, available stock, invoice state, and other volatile values. These are not copied into Store Intelligence as truth; they must be requested from the approved operational Gateway when integration is authorized.

The model receives only the normalized, allowlisted projection. It never receives arbitrary SQL, raw table definitions, unrestricted database rows, or credentials.

## 3. Proposed data contract (draft)
- `StoreCatalogProfile`: store ID, profile ID/version, source schema version, mapping status, approved timestamp, and mapping provenance.
- `CatalogProduct`: store-scoped stable product ID, canonical name, category path, unit, active flag, and source version.
- `ProductAttributeDefinition`: namespaced key, display label, value type, allowed values/constraints, sensitivity classification, and version.
- `ProductAttributeValue`: product ID, attribute key, typed value, source field/reference, and observed timestamp.
- `ProductAlias`: normalized alias, language/locale, source, confidence, and approval status.

These are proposed shapes, not frozen contracts. No migration or Hub schema change is authorized by this document.

## 4. Discovery and mapping policy
A future adapter may inspect a source schema through an allowlisted read-only discovery interface and produce a **mapping proposal**. It must not auto-approve or auto-execute a mapping.
- Known fields map to canonical fields only through a versioned mapping profile.
- Unknown fields are quarantined for review, not sent directly to the model.
- PII and sensitive fields are excluded by default; field names alone are not enough to infer safety.
- Aliases and business-specific vocabulary require provenance and an explicit approval state.
- A source schema version change invalidates the previous mapping until compatibility is checked.
- Any mapping must be reproducible and auditable.

## 5. Key technical risks
- **Schema drift:** source columns and semantics change over time.
- **Semantic mismatch:** two stores may use the same field name for different meanings.
- **Attribute explosion:** unbounded free-form attributes create poor retrieval and prompt bloat.
- **PII leakage:** customer or staff fields may be present in source schemas.
- **Stale operational values:** price and stock must not be answered from a stale catalog projection.
- **Cross-store leakage:** every product, alias, profile, and retrieval operation must remain store-scoped.
- **False confidence:** an automatically inferred mapping must never be presented as verified store truth.

## 6. Scope allowed before Hub approval
The AI repository may implement:
- canonical and extensible contracts;
- fixture catalogs representing at least three distinct store types;
- mapping proposal/validation logic over synthetic schemas;
- alias normalization and deterministic product retrieval;
- tests for tenant isolation, schema drift, unknown-field quarantine, PII exclusion, and live-value separation;
- documentation and exportable mapping proposals.

It may NOT implement:
- a live Hub adapter or connection string handling;
- access to real store databases or production credentials;
- changes to the parent Kandoo repository, SQLite schema, operation log, or migrations;
- automatic production mapping approval or write operations.

## 7. Proposed acceptance gates
1. At least three synthetic store catalogs with materially different attribute sets use the same canonical contract.
2. Product-specific attributes round-trip without per-store code changes.
3. Unknown attributes remain quarantined until explicitly allowlisted.
4. PII-like fields are excluded by default and tested negatively.
5. Current price and stock are never sourced from the descriptive catalog projection.
6. Store A cannot query or receive Store B products, aliases, profiles, or evidence.
7. Schema version drift is detected and blocks use of an unapproved mapping.
8. Alias search is deterministic and carries provenance/approval state.
9. Ambiguous retrieval produces clarification rather than arbitrary selection.
10. CI, source archive, manifest, and SHA-256 verification pass.

## 8. Governance and approval
- This document remains the technical scope and acceptance basis; Product Owner approval authorizes only synthetic fixture implementation.
- Product Owner approval was granted on 2026-11-12 for fixture-only implementation in the AI repository.
- Kandoo Project Manager approval is separately required before any live Hub integration or parent-repository change.
- A successful synthetic implementation does not satisfy or waive the parent Kandoo blockers (write-path closure, consistent snapshot, change feed, identity, privacy, restore/rebuild semantics).

## 9. Recommendation
Proceed with a fixture-only Build 0.3 after Product Owner approval. Keep the adapter boundary abstract and do not select a database provider or deploy location until the Project Manager approves the Hub integration design.


## 10. Implementation boundary after approval
The first implementation slice lives on branch `build/0.3-dynamic-catalog`. It adds canonical/extensible contracts, three synthetic catalog fixtures, mapping proposal validation, PII-like field exclusion, unknown-field quarantine, schema-drift invalidation, and store-scoped deterministic retrieval. These are test fixtures, not a production adapter. Build/CI evidence is recorded in the handoff only after the corresponding run is verified.


## 11. Typed attribute validation implementation
The fixture implementation now includes store-specific attribute definitions and a schema validator. It checks definition existence, allowlist and sensitivity, type agreement, exactly one correctly typed payload, and allowed-value constraints. Synthetic tests cover valid catalogs, wrong type/multiple payloads, disallowed values, and sensitive/non-allowlisted definitions.


## 12. Ambiguity-safe catalog query resolution
The catalog query service returns one of Resolved, ClarificationRequired, or NotFound. When multiple products match, it returns candidate identities and a clarification question but no selected product or evidence object. A single match returns the product and store-scoped evidence. Synthetic tests cover broad ambiguous queries, approved alias resolution, unapproved aliases, and cross-store isolation.
