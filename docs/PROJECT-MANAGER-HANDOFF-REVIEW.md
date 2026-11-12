# Kandoo Internal AI — Project Manager Technical Handoff Review

**Date:** 2026-11-12  
**Repository:** https://github.com/majid-ghanbari-dost/kando-internal-ai-v01  
**Review scope:** Independent AI repository only; fixture-backed implementation  
**Prepared by:** Technical Manager / Architecture Review  
**Decision requested:** Review the independent repository architecture and test evidence. This document does not request Hub integration approval.

## 1. Executive status

The independent AI repository has progressed through Builds 0.1–0.4. Build 0.4 is complete within its approved fixture-only scope. The HTTP Assistant path can route descriptive product-attribute questions to a typed catalog query tool for three synthetic store profiles.

The repository is **not production-ready** and is **not connected to Kandoo Hub**. The Host has no authentication or authorization and accepts StoreId in the request body. It must remain local fixture-only and must not be exposed to a network or real store environment.

All PRs #1–#4 are open and unmerged. PRs are stacked; no code has been merged to main. The handoff document is mirrored to main as documentation only.

## 2. Delivery status

| Build | Status | Evidence / remaining work |
|---|---|---|
| 0.1 foundation | Complete | .NET 8 solution, minimal API, allowlisted legacy lookup tool, fixture gateway, CI |
| 0.2.1 Persian query evaluation | Complete | Versioned synthetic corpus and parser tests |
| 0.2.2 ambiguity-safe clarification | Complete | Explicit resolution states and regression tests |
| 0.3 dynamic catalog | 100% fixture-only | Canonical contracts, three synthetic profiles, typed attributes, mapping proposal generation, quarantine, PII-like exclusion, schema-drift invalidation, approved aliases, scoped retrieval; portable proposal export completed by Build 0.5 |
| 0.4 Assistant catalog integration | 100% fixture-only | HTTP path reaches catalog tool; typed attributes/evidence; ambiguity and tenant isolation; 36/36 tests passed |
| 0.5 Mapping proposal export | 100% fixture-only | Canonical v1 JSON envelope, deterministic proposal ID/SHA-256, strict verified import, three synthetic examples; 50/50 tests passed; Manifest 46/46 independently verified |

The overall operational product remains substantially incomplete: no real LLM provider, authentication/authorization, Hub adapter, real store data, production deployment or operational validation.

## 3. Build 0.4 verified implementation

- IAssistantCatalogTool is a narrow typed contract. Legacy IAssistantTool / ProductLookupResult remains unchanged.
- CatalogQueryTool wraps CatalogProductQueryService.
- Host registers fixture repository, query service and catalog tool.
- FakeModelProvider routes a small fixed set of Persian attribute markers deterministically. This is not semantic LLM capability.
- AssistantService dispatches catalog.product.describe explicitly and handles Resolved, ClarificationRequired and NotFound.
- Resolved responses carry product identity, typed attribute slots, source-field provenance, profile/version evidence and fixture mode.
- Ambiguous responses contain candidate identities and a clarification question only; they do not return a selected product or catalog evidence.
- Descriptive catalog responses do not contain current price or stock.
- HTTP tests cover party supplies, grocery, clothing, typed decimal diameter, ambiguity, not-found/cross-store behavior and legacy lookup regression.

### CI evidence

Latest verified Build 0.5 commit: 50567aa08205b528e6fc338ed389a990b97184ae

- CI Run: 43216967966 — Success
- Tests: 50/50 passed, 0 failed
- Source Artifact ID: 13943373828; digest sha256:70eb62fd75be5a7a9f8a892f61d890479967905632776ba68b0f8993d7e1847f
- Test Artifact ID: 13943159118; digest sha256:ce9d2edb29c7b10dbe36626cad22482a670cee9e79067f0bafeccb31551ec667
- Source archive internal tar.gz SHA-256: 27b71e2670d0fcbbe0acf853ef890677d72f16c412d3c6d3f2dd3cf59583dc09
- Manifest: 46/46 entries independently verified
- CI URL: https://github.com/majid-ghanbari-dost/kando-internal-ai-v01/actions/runs/43216967966

This CI-tested commit includes the Build 0.5 implementation, tests, examples, README, challenge, and Handoff v1.35. The Project Manager review document is being refreshed as a separate governance artifact; the PR page remains the source for live branch head and merge state.

## 4. PR and branch state

| PR | Scope | Base → Head | State |
|---|---|---|---|
| #1 | Build 0.2.1 | main → build/0.2.1-query-evaluation | Open, unmerged |
| #2 | Build 0.2.2 | build/0.2.1-query-evaluation → build/0.2.2-clarification | Open, unmerged |
| #3 | Build 0.3 | build/0.2.2-clarification → build/0.3-dynamic-catalog | Open, unmerged |
| #4 | Build 0.4 | build/0.3-dynamic-catalog → build/0.4-assistant-catalog-integration | Open, unmerged |
| #5 | Build 0.5 | build/0.4-assistant-catalog-integration → build/0.5-mapping-proposal-export | Open, unmerged; mergeable true |

No review submissions or inline review threads were present for PR #3 or PR #4 at the time of this check. No PR was merged or closed.

## 5. Architecture and security boundaries

### Confirmed safeguards in the fixture implementation
- Store-scoped fixture profiles, products, aliases, searches and evidence.
- Only approved aliases are searchable.
- Unknown mapping fields are quarantined.
- PII-like source field names are excluded by the current conservative name-token filter.
- Attribute definitions enforce allowlist, public sensitivity, type and allowed-value constraints.
- Schema version drift marks a proposal Invalidated.
- Current price/stock are not part of CatalogProduct.
- No arbitrary SQL, shell, filesystem, network or write tool is exposed.

### Material limitations / risks
1. **No authentication or authorization:** AssistantRequest.StoreId is supplied by the caller. A real service must derive tenant identity from an authenticated principal, not trust request-body StoreId.
2. **Local-only Host:** no auth middleware or network security boundary exists. Do not expose this Host to LAN/public network.
3. **No real LLM:** FakeModelProvider is a deterministic rule-based test double.
4. **Tool dispatch split:** legacy lookup uses IAssistantToolRegistry; catalog uses a separate interface and explicit branch. This is bounded for Build 0.4 but needs a deliberate unified policy/dispatch design before external model planning.
5. **Basic retrieval:** catalog matching uses normalized substring matching. Semantic ranking, typo tolerance, relevance thresholds and multi-turn clarification state are absent.
6. **No authenticated review/activation workflow:** Build 0.5 now exports and verifies a portable proposal, but it deliberately does not authenticate a reviewer, persist an approval, or activate a mapping.
7. **PII screening is not sufficient for production:** field-name heuristics are defense-in-depth only. A production mapping process requires explicit reviewed source classification and allowlist approval.
8. **No persistence or restore semantics:** fixtures are static. No durable Store Intelligence store, snapshot, change feed, checkpoint or rebuild mechanism exists.
9. **No live operational reads:** current price, stock and invoice state are not wired to a live Gateway.
10. **No observability/security operations:** authentication, authorization, rate limiting, audit, secret management, deployment hardening and incident handling are not implemented.

## 6. Parent Kandoo Hub blockers — separate project boundary

The parent Kandoo repository audit identified incomplete write-path closure. In particular, CompleteTimeout can update invoice/DraftLock state through a direct transaction path without producing an Operation Log entry; direct write wrappers also require a complete call-site audit.

Before any live AI ingestion is approved, the Kandoo Project Manager must close or explicitly account for:
- all operational write paths and Operation Log coverage;
- consistent snapshot/high-water-mark semantics;
- paginated and integrity-checked Change Feed from Replication Log;
- store identity and authorization;
- PII minimization/redaction, including invoice payloads;
- restore/rebuild/checkpoint semantics;
- proof that volatile price/stock are read live, not from stale projection.

This AI repository review does not waive any parent-repository blocker.

## 7. Recommended decisions

1. **Accept Build 0.4 as a successful fixture-only implementation slice**, subject to normal code review; do not describe it as a production-ready assistant.
2. **Do not approve Hub integration yet.** Mission 4.1 and the snapshot/change-feed/identity/privacy blockers remain.
3. Accept Build 0.5 as complete in the fixture-only scope after ordinary PR review; keep all mapping approvals and activation out of this build.
4. Before connecting an external LLM, require a separate security/tool-planning challenge covering untrusted model output, tool argument validation, tenant binding, prompt injection, timeouts, budgets, audit and deny-by-default behavior.
5. Keep all PRs unmerged until the Product Owner explicitly decides the merge sequence.

## 8. Proposed next step

Build 0.5 is complete in the fixture-only scope. The next useful boundary is a **review-decision record**, not automatic mapping activation. A future Build 0.6 challenge should define a decision artifact bound to the exact proposal ID and content digest, with explicit Approved / Rejected / ChangesRequested outcomes, reviewer reference, timestamp and reason. Verification must reject stale or cross-store decisions. The implementation should remain fixture-only and must not persist or activate mappings until authentication, authorization and the Project Manager's separate Hub integration gate are satisfied.

Technical Challenge: to be drafted as `docs/BUILD-0.6-HUMAN-MAPPING-REVIEW-CHALLENGE.md`.  
Status: Not approved; no Build 0.6 implementation has started.
