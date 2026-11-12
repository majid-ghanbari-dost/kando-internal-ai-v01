# Kandoo Internal AI — Build 0.1

A small, model-independent assistant core for Kandoo store workflows.

## Status
This build uses fixture data and a deterministic fake model provider only. It is not connected to a live Kandoo Hub, does not read a real store database, and must never be represented as production/live data.

## Architecture
- **Core:** request, plan, product and evidence contracts.
- **Application:** assistant orchestration, tool boundary and product lookup use case.
- **Infrastructure:** fixture-backed store gateway, fake provider and explicit tool registry.
- **Host:** minimal HTTP API.
- **Tests:** isolation, evidence, abstention and tool allowlist tests.

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
{ "storeId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "question": "بادکنک لاتکس قرمز" }
```

The response labels its source as `fixture` and includes evidence identifiers. Fixture timestamps are fixed test data, not current inventory timestamps.

## Security and scope
- Product queries are scoped by StoreId in the gateway.
- No customer data or phone numbers exist in product fixtures.
- Unknown tool names are rejected.
- No arbitrary SQL, shell, filesystem, network or write tool is exposed.
- Price and inventory are illustrative fixture values.
- Live Hub integration is blocked until parent Kandoo write-path, snapshot, change-feed and privacy contracts pass technical challenge.
