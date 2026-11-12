# Build 0.7 — Trust, Authorization, Decision Registry and Mapping Activation (Technical Challenge)

**Status:** DRAFT — architecture challenge only; not approved; no implementation started  
**Date:** 2026-11-12  
**Base:** Build 0.6 fixture-only decision artifacts  
**Repository:** `kando-internal-ai-v01`

## 1. Why this is the next boundary

Build 0.6 can create and verify a portable human review record bound to one exact proposal. It deliberately does not authenticate the reviewer, persist decisions, enforce authorization, revoke decisions, or activate a mapping.

Those are not serialization problems. They are trust, identity, authorization, durable-state, concurrency, and operational-safety problems. They must be designed as a coherent boundary before any Hub connection or production write path is considered.

## 2. Goal

Produce a reviewable architecture and contract package that answers:

1. Who is the authenticated actor, and which trusted component establishes that identity?
2. Which store is the actor acting for, and how is cross-store access prevented?
3. Which distinct capabilities are required to review, approve, activate, revoke, and audit a mapping?
4. How is a review decision durably recorded, superseded, revoked, and audited?
5. How is an approved mapping activated safely against the exact profile/schema version?
6. How can activation be rolled back without corrupting Store Intelligence or interrupting ordinary store operations?

The model and UI are never authorities. They may request or recommend actions; a trusted server-side policy boundary decides whether the authenticated actor may perform them.

## 3. Required architecture decisions

### 3.1 Identity and trust source

- Inventory the actual identity/authentication capabilities already present in Kandoo Hub and its supported clients before selecting an identity design.
- Do not assume an external identity provider, token format, user directory, or role model exists.
- Define the trusted issuer, token audience, expiry, subject, session/device context, store binding, and signature/key rotation expectations.
- Client-supplied `reviewer_ref`, StoreId, role, or permission claims are untrusted unless bound to a verifiable trusted credential.
- The AI model must never create, infer, elevate, or fabricate identity claims.

### 3.2 Store-scoped authorization

Define server-enforced capabilities at minimum:

- `catalog.mapping.view`
- `catalog.mapping.review`
- `catalog.mapping.approve`
- `catalog.mapping.activate`
- `catalog.mapping.revoke`
- `catalog.mapping.audit`

Specify role-to-capability mapping, least privilege, store scope, expiry, denial behavior, and separation of duties. Decide whether the same actor may both approve and activate; default proposal is **no**, unless Product Owner explicitly accepts a documented small-store exception.

Authorization must be re-evaluated at the operation boundary, not trusted from a prior UI check or an LLM tool selection.

### 3.3 Durable decision registry

Design a durable record bound to:

- exact proposal ID and proposal content digest;
- StoreId, ProfileId, profile version, and source schema version;
- decision outcome and reason;
- authenticated actor subject and authorization context;
- server-recorded timestamp and request/correlation ID;
- previous/superseded/revoked decision references;
- contract and policy version.

Define uniqueness/idempotency rules, concurrency handling, append-only audit semantics, retention, backup/restore behavior, and duplicate/replay detection. Do not treat the portable Build 0.6 JSON artifact as the authoritative registry.

### 3.4 State machine and activation

Specify explicit state transitions, at minimum:

`Proposed → Reviewed → Approved → ActivationPending → Active`

with terminal/side paths for `Rejected`, `ChangesRequested`, `ActivationFailed`, `Revoked`, and `Superseded`.

- Approval alone must not activate a mapping.
- Activation must require a separate authorized action.
- Activation must compare the current profile version and schema fingerprint with the exact reviewed proposal; stale proposals fail closed.
- Activation must be idempotent and atomic with the generation switch/checkpoint.
- Build a new Store Intelligence generation, validate it, then switch atomically; retain the previous generation until success.
- Define rollback and revocation semantics, including how queries stop using a revoked mapping.
- Never let Store Intelligence become the operational source of truth.

### 3.5 Audit and recovery

- Define append-only audit events for review, approval, activation, failure, revocation, supersession, and rollback.
- Evaluate whether Kandoo's Operation Log can safely host these events only after its write-path audit is complete; do not assume the current Log covers every operational path.
- Define hash/integrity checks, idempotency, crash recovery, restore behavior, and reconciliation of registry state with active projection generation.
- An audit gap or inconsistent checkpoint must fail closed for mapping activation, without blocking normal store operations.

## 4. Threat model required

Cover at least:

- forged or replayed reviewer identity;
- cross-tenant/store ID substitution;
- privilege escalation and role confusion;
- self-approval/self-activation;
- proposal tampering and stale schema/profile;
- duplicate requests and replay;
- compromised or prompt-injected model attempting privileged tool calls;
- client/UI bypass;
- audit tampering or deletion;
- clock skew, expired credentials, key rotation;
- crash between approval, activation, checkpoint and generation switch;
- restore from an older Hub backup;
- revoked/superseded mapping continuing to affect retrieval.

For each threat, document trust boundary, exploit path, mitigation, residual risk, and test evidence needed.

## 5. Required deliverables after separate approval

- Architecture Decision Records (ADRs) for identity, authorization, decision registry, state machine, activation, audit, and rollback.
- Trust-boundary and data-flow diagrams.
- Role/capability matrix and separation-of-duties matrix.
- Versioned API/data contracts with idempotency and concurrency semantics.
- Threat model and abuse-case test plan.
- Failure/recovery/rollback matrix.
- Dependency register identifying unresolved Kandoo Hub capabilities.
- Implementation plan split into independently reviewable Build steps.

## 6. Explicit non-goals / prohibitions

Until a separate Build challenge is approved:

- No implementation, migrations, schema changes, or Hub adapter.
- No live Hub connection, real store DB, production credentials, or external identity provider setup.
- No mapping persistence, activation, revocation, or production authorization.
- No changes to the parent Kandoo repository.
- No LLM authority or model-managed permission decisions.
- No PR merge without Product Owner approval.

Mission 4.1 Write-Path Audit in the parent Kandoo repository remains an important dependency. The audit must be completed and reviewed before any production audit/activation design assumes Operation Log completeness.

## 7. Acceptance criteria for the architecture package

1. Existing Hub identity and authorization capabilities are evidenced, or explicitly recorded as absent/unknown.
2. Every privileged action has an authenticated principal, store scope, capability, and server-side enforcement point.
3. Review, approval, activation, revocation, and audit are distinct permissions/actions.
4. A proposal decision is immutable and bound to exact proposal/profile/schema versions.
5. Duplicate/replay, stale proposal, cross-store access, self-approval and revocation are specified.
6. Activation and rollback have atomicity, idempotency, recovery and fail-closed semantics.
7. The AI model is outside the trust authority boundary.
8. Operation Log limitations and Mission 4.1 dependency are explicitly handled.
9. No unsupported claim about production readiness or existing Hub capability remains.
10. Architecture package passes a separate Technical Challenge and Product Owner review.

## 8. Approval gate

This document is a **draft Technical Challenge only**. It does not authorize implementation or any production access. The Product Owner must explicitly approve the exact Build 0.7 scope before implementation begins. Build 0.6 approval does not authorize Build 0.7, parent-repository changes, Hub integration, identity-provider selection, migrations, or mapping activation.
