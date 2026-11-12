# Kandoo Internal AI

A model-independent assistant core for Kandoo store workflows.

## Current build
Build 0.3 is in progress on branch `build/0.3-dynamic-catalog`. Its first implementation slice introduces a shared canonical product contract, typed extensible attributes, three synthetic store catalogs (party supplies, grocery, clothing), versioned mapping proposals, default quarantine/exclusion rules, and deterministic store-scoped retrieval. Build 0.2.2 retains explicit ambiguity handling; Build 0.2.1 provides the synthetic Persian query corpus. The parser remains a deterministic rule-based baseline. No live Hub or real store data is connected.

## Architecture
- **Core:** request, intent, plan, product and evidence contracts.
- **Application:** assistant orchestration, Persian text normalization, product query parsing and tool boundary.
- **Infrastructure:** fixture-backed store gateway, deterministic fake provider and explicit tool registry.
- **Host:** minimal HTTP API.
- **Tests:** unit, parser and HTTP integration tests.

Only registered tools can execute. Model-generated SQL or code is never executed.

## Run
Requires .NET 8 SDK.
```powershell
dotnet restore .\Kandoo.InternalAI.sln
dotnet test .\Kandoo.InternalAI.sln --configuration Release
dotnet run --project .\src\Kandoo.InternalAI.Host
```

## API
- `GET /health`
- `POST /assistant/ask`

Example:
```json
{ "storeId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "question": "قیمت بادکنک لاتکس قرمز چنده؟" }
```

Responses include structured product data and evidence identifiers. Ambiguous searches return `resolutionStatus=clarification_required`, a clarification question, and candidate identity fields; they do not return a selected product or final price/stock. The response labels its source as `fixture`; fixture timestamps and prices are not live store values.

## Persian query handling
The current deterministic baseline:
- normalizes Arabic/Persian Yeh and Kaf;
- normalizes Persian and Arabic-Indic digits to ASCII;
- normalizes ZWNJ/ZWJ and whitespace;
- identifies basic price, stock and product-details intents;
- removes common request filler words before fixture lookup.

This is not semantic language understanding. The parser now removes request phrases only at query boundaries so words inside a product phrase are preserved. A synthetic corpus checks exact intent and query extraction. The corpus is not representative of real store language; spelling correction, synonym/alias expansion, ranking and clarification behavior remain future work.

## Query evaluation
- Corpus: `tests/Kandoo.InternalAI.Tests/Fixtures/persian-query-corpus.v1.json`.
- Version: `kandoo-fa-product-query-v1` / v1, synthetic `fa-IR`, no customer or personal data.
- Automated test checks exact intent and exact extracted product phrase for every case; the CI result is the acceptance evidence.
- Synthetic accuracy must not be presented as real-world language accuracy.

## Security and scope
- Product queries are scoped by StoreId in the gateway, but the Host does not yet authenticate or authorize StoreId. Keep it local and do not expose it to a network.
- No customer data or phone numbers exist in product fixtures.
- Unknown tool names are rejected.
- No arbitrary SQL, shell, filesystem, network or write tool is exposed.
- Price and inventory are illustrative fixture values.
- Live Hub integration is controlled by the Kandoo Project Manager. This repository may implement and test provider-independent contracts, fake gateways, adapters against test doubles, and handoff documentation only. No live Hub connection, Hub database access, production credentials, schema migration, or operational read/write is permitted until the Project Manager reviews the repository and explicitly approves the integration.
- Any future Hub integration must first close the parent Kandoo write-path, snapshot, change-feed, identity, and privacy blockers.

## Ambiguity and clarification (Build 0.2.2)
- A single product match returns a normal resolved product answer.
- Zero matches return `resolutionStatus=not_found`.
- More than one match returns `resolutionStatus=clarification_required`, a Persian clarification question, stable candidate IDs/names, and candidate evidence.
- During clarification, `Products` is empty: the assistant does not choose a candidate or expose a final price/stock answer for an unresolved query.
- Candidate ordering is deterministic by product name and product ID.
- To resolve a candidate in this stateless prototype, the operator must restate the request with the full product name or its product code (e.g. `قیمت P-101`). No conversation/session state is persisted.
- The behavior is validated against the fixture gateway only. It is not evidence of live-store search quality.

## Dynamic Store Catalog (Build 0.3)
- One canonical `CatalogProduct` contract is shared by party-supply, grocery and clothing fixtures; store-specific properties live in typed, namespaced attributes.
- `StoreCatalogProfile` is store-scoped and versioned. Mapping discovery emits a proposal and never approves it automatically.
- Unknown source fields are quarantined. PII-like field names are excluded by default. Only explicitly allowlisted, public and type-compatible fields are proposed for mapping.
- Schema-version drift marks the proposal invalidated and records a validation issue.
- Aliases carry store, source, locale, confidence and approval status; retrieval uses approved aliases only.
- Search and evidence are store-scoped and deterministic.
- Descriptive catalog contracts contain no current price, stock or invoice fields. Those remain live operational data and are not answered from Store Intelligence.
- All current Build 0.3 catalog data is synthetic fixture data; no database adapter or live integration exists.

## Hub integration governance
- The Kandoo Project Manager owns the integration gate.
- The AI team may continue independent development, synthetic testing, and adapter-contract preparation.
- No live Hub connection, real Hub data access, credentials, schema/code changes in the parent Kandoo repository, migrations, or operational actions may occur without explicit Project Manager approval after repository handoff and review.
