# Kandoo Internal AI

A model-independent assistant core for Kandoo store workflows.

## Current build
Build 0.2.1 adds a versioned synthetic Persian query corpus and corpus-driven parser regression tests. The parser remains a deterministic rule-based baseline. The project still uses fixture data and a fake provider; it is not connected to a live Kandoo Hub and is not an autonomous AI model.

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

Responses include structured product data and evidence identifiers. The response labels its source as `fixture`; fixture timestamps and prices are not live store values.

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
- Live Hub integration is blocked until parent Kandoo write-path, snapshot, change-feed, identity and privacy contracts pass technical challenge.
