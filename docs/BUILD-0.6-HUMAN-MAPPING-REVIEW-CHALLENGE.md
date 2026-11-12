# Build 0.6 — Human Mapping Review Decision Record (Technical Challenge)

**Status:** APPROVED BY DELEGATED TECHNICAL DECISION — fixture-only implementation in progress  
**Date:** 2026-11-12  
**Scope proposal:** Independent Kandoo Internal AI repository; synthetic fixtures only

## 1. Problem

Build 0.5 produces a deterministic, integrity-checked Mapping Proposal artifact. It intentionally does not record a human review decision. The next safe boundary is to represent a review decision against the exact immutable proposal content without activating or persisting a mapping.

## 2. Goal

Define a versioned, auditable decision artifact that answers:
- Which exact proposal was reviewed?
- Which store and profile did it concern?
- Was it approved, rejected, or returned for changes?
- Which reviewer reference made the decision, when, and why?
- Can the decision be verified against the exact proposal digest?

A decision must never apply a mapping or make an unverified proposal active.

## 3. Proposed contract

A future `kandoo.catalog-mapping-review-decision` v1 envelope may contain:
- contract and contract_version;
- deterministic decision_id;
- proposal_id and proposal_content_sha256;
- store_id and profile_id;
- decision: `Approved`, `Rejected`, or `ChangesRequested`;
- opaque reviewer_ref (synthetic fixture actor only in this build);
- reviewed_at_utc in normalized UTC form;
- reason (review text, no secrets or personal data);
- decision_content_sha256.

The decision must bind to both proposal_id and proposal_content_sha256. A changed proposal must require a new review decision. Cross-store or mismatched profile references must be rejected.

## 4. Proposed verification behavior

- Accept only a previously verified Build 0.5 proposal artifact.
- Verify the proposal contract and digest before creating or checking a decision.
- Reject mismatched proposal ID, content digest, store ID, or profile ID.
- Reject unknown/duplicate fields and non-canonical JSON.
- Ensure decisions are explicit and from the fixed enum; no default approval.
- Preserve reason and reviewer reference as metadata, but do not treat reviewer_ref as authenticated identity.
- Ensure deterministic decision IDs and integrity digests under a documented canonicalization rule.
- Re-review is required if proposal content changes.
- No automatic transition from proposal to active mapping.

## 5. Scope allowed after separate approval

- Add Core contracts for a review-decision artifact.
- Add deterministic serializer/import verifier using built-in .NET libraries.
- Add fixture-only examples for the three synthetic store profiles.
- Add tests for decision binding, digest tampering, stale proposal, cross-store mismatch, missing/unknown fields, duplicate decisions, and no-default-approval.
- Document the difference between a review record and production authorization.

## 6. Explicit non-goals / prohibitions

- No authentication, identity provider, or claim that fixture reviewer_ref is a real user identity.
- No durable approval database, mapping persistence, activation, or migration.
- No live Hub, real store database/schema, production credentials, or parent-repository changes.
- No mapping UI, LLM, external API, or operational write tool.
- No PR merge without Product Owner decision.

## 7. Acceptance criteria

1. A decision references one exact verified proposal ID and content digest.
2. Mismatched store/profile/proposal/digest is rejected.
3. Changed proposal content cannot reuse an earlier decision.
4. Approved, Rejected and ChangesRequested are distinct and explicit.
5. No decision is inferred or defaulted.
6. Deterministic decision export/import and SHA-256 verification pass.
7. Unknown/duplicate fields and tampering are rejected.
8. All examples and tests use synthetic data only.
9. No decision persists or activates a mapping.
10. CI, source archive, manifest and SHA-256 verification pass.

## 8. Approval and implementation boundary

On 12 November 2026, the Product Owner again delegated the technical decision with: “بهترین تصمیم فنی را خودت بگیر”. The Technical Manager accepted the exact fixture-only scope in this challenge and started implementation on branch `build/0.6-human-mapping-review`.

This delegated decision authorizes only the isolated fixture-only work described above. It does not authorize real reviewer authentication, production approval, durable storage, Hub integration, mapping activation, parent-repository changes, or PR merge. Those remain separately gated.
