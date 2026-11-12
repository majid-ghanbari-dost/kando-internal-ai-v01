# Build 0.4 — Dynamic Catalog Tool Integration into Assistant

Status: APPROVED FOR FIXTURE-ONLY IMPLEMENTATION by Product Owner on 2026-11-12
Date: 2026-11-12
Owner: AI project team; Hub integration remains under separate Kandoo Project Manager gate

## 1. Purpose

Connect the Build 0.3 dynamic catalog query capability to the existing Assistant request path so a store operator can ask about descriptive product attributes through the normal `POST /assistant/ask` endpoint.

This is an AI-repository-only, fixture-backed integration. It does not authorize Hub access, real store data, external model providers, production credentials, migrations, or write operations.

## 2. Current code boundary (verified)

- `AssistantService` accepts only `ProductLookupTool.ToolName` and executes the legacy `ProductLookupTool`.
- `IAssistantTool.ExecuteAsync` currently returns the legacy `ProductLookupResult`, which is based on `ProductReadModel`.
- `FakeModelProvider` always plans `product.lookup` through the deterministic Persian parser.
- `CatalogProductQueryService` is implemented and tested, but is not registered in the Host and is not called by `AssistantService`.
- `AssistantToolRegistry` is an allowlist registry; unknown tool names are rejected.
- The Host currently registers only `ProductLookupTool` and `FakeStoreReadGateway`.

## 3. Required operator experience

Examples for fixture-only tests:
- "قطر بادکنک فویلی ستاره‌ای چقدر است؟"
- "جنس بادکنک قرمز چیست؟"
- "سایز شلوار جین چیست؟"
- A broad query such as "مشخصات بادکنک" must ask which product is intended when multiple products match.
- A product belonging to another StoreId must never be returned.

Responses must identify fixture data and provide product identity, typed descriptive attributes, and provenance/evidence. They must not invent current price or stock from the descriptive catalog.

## 4. Architecture decision — selected Option B

The current tool interface is coupled to `ProductLookupResult`. Do not simply register the new service and cast or discard data.

Evaluate and select one of these bounded options:

A. Introduce a common discriminated `AssistantToolResult` envelope with explicit result kinds and typed payloads, then adapt the existing ProductLookupTool and new CatalogQueryTool.

B. Introduce a narrow catalog-specific tool contract and explicit typed dispatch while retaining the existing ProductLookupTool contract. This reduces immediate blast radius but may create multiple tool interfaces.

**Selected: Option B.** Preserve the existing IAssistantTool / ProductLookupResult contract and add a narrow IAssistantCatalogTool contract returning the existing strongly typed CatalogQueryResult. The Assistant explicitly dispatches the catalog tool name to that interface. No arbitrary object payload or reflection-based discovery is introduced. This keeps the legacy lookup path stable and bounds the Build 0.4 change.

The registry must remain an explicit allowlist. No reflection-based discovery of arbitrary tools, dynamic assembly loading, arbitrary SQL, shell, filesystem, or network tools.

## 5. Scope after approval

- Implement the selected typed tool contract.
- Add a fixture-backed catalog query tool wrapping `CatalogProductQueryService`.
- Extend deterministic FakeModelProvider routing for catalog-description questions without claiming semantic LLM capability.
- Preserve the existing product lookup behavior and Build 0.2.2 ambiguity behavior.
- Add catalog query service registration to the Host.
- Return a structured response with resolution status, candidates when ambiguous, typed attributes when resolved, and evidence/provenance.
- Ensure current price and stock are omitted from catalog-description responses.
- Add HTTP end-to-end tests for resolved attribute lookup, ambiguous query, not found, cross-store isolation, unknown tool rejection, and legacy product lookup regression.
- Update README and handoff.

## 6. Explicit non-goals and prohibitions

- No real Hub adapter or connection string.
- No real store DB, production data, or credentials.
- No parent Kandoo repository changes, schema changes, migrations, or write tools.
- No real LLM provider selection or external API call.
- No authentication claim; Host remains fixture-only and local-use only.
- No live price/stock answer from catalog projection.
- No merge of PR #3 or other PRs without Product Owner approval.

## 7. Acceptance criteria

1. A descriptive catalog query can travel through `POST /assistant/ask` to the allowlisted catalog tool.
2. Resolved results return canonical product identity, typed descriptive attributes, and provenance.
3. Ambiguous results return candidates and clarification only; no selected product or final answer.
4. Not-found and unsupported-tool states remain distinct.
5. StoreId isolation is verified at HTTP boundary.
6. Legacy `product.lookup` tests remain green.
7. No price/stock is inferred from catalog descriptive data.
8. Tool result contracts are strongly typed and do not permit arbitrary executable payloads.
9. Host registration and deterministic fixture routing are covered by integration tests.
10. CI, source archive, manifest, and SHA-256 verification pass.

## 8. Gate

Product Owner approval was explicitly received on 2026-11-12 for implementation within this AI repository, limited to the fixture-only scope above. Implementation may proceed on build/0.4-assistant-catalog-integration. Hub integration remains separately blocked until the Kandoo Project Manager independently approves it.


## 9. Implementation outcome (2026-11-12)

Build 0.4 fixture-only implementation is complete on branch build/0.4-assistant-catalog-integration.

- Selected architecture: Option B; the legacy ProductLookup tool contract remains unchanged and a separate IAssistantCatalogTool contract is used for descriptive catalog queries.
- The Host registers the fixture repository, catalog query service and catalog tool.
- Deterministic FakeModelProvider routes known descriptive-attribute markers to catalog.product.describe. This is a rule-based fixture route, not a claim of LLM semantic understanding.
- Assistant responses include typed attribute value slots, source-field provenance, profile/version identity and evidence.
- Ambiguity returns candidates and clarification only; not-found remains distinct; cross-store isolation is tested.
- Current price and stock are absent from the descriptive catalog response.
- HTTP integration tests cover party supplies, grocery, clothing, ambiguity, not-found/cross-store behavior and legacy lookup regression.
- CI Run 43208602845 on commit 56007c0557b50fc04767fae84d96a35e517373a8 passed: 36/36 tests, 0 failed; manifest 37/37 verified.
- PR #4: https://github.com/majid-ghanbari-dost/kando-internal-ai-v01/pull/4 (open, unmerged).
- No live Hub, real store DB, external LLM, production credential, migration, parent-repository change or write tool was introduced.
