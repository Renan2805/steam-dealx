# SteamDealX

REST API that aggregates Steam game pricing data from multiple sources — [gg.deals](https://gg.deals) (retail & keyshop prices) and [IsThereAnyDeal (ITAD)](https://isthereanydeal.com) (per-store prices, historical lows, active bundles) — and returns a unified, normalized response.

## Architecture

```
SteamDealX.Api            → ASP.NET Core controllers, rate limiting, OpenAPI
SteamDealX.Infrastructure → Orchestration, caching, EF Core / SQLite
SteamDealX.Clients        → HTTP clients for gg.deals and ITAD APIs
SteamDealX.Core           → Domain models and interfaces
```

**Caching:** Two-tier hybrid cache — 10-minute in-memory (L1) + 2-hour SQLite (L2). Batch endpoints check the cache per-ID and only fetch what's missing.

## Endpoints

All endpoints accept an optional `?region=` query parameter (default: `br`).

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/games/{steamAppId}` | Prices for a single game |
| `GET` | `/games/batch?ids=730&ids=440` | Batch prices (up to 100 games) |
| `GET` | `/games/search?title=Counter-Strike` | Search by title |
| `GET` | `/subs/{steamSubId}` | Prices for a package/sub |
| `GET` | `/bundles/{steamBundleId}` | Prices for a bundle |
| `GET` | `/steam/{type}/{id}` | Unified endpoint (`type` = `app`\|`sub`\|`bundle`) |
| `GET` | `/health` | Health check |

Interactive docs are available at `/scalar`.

## Running Locally

**Prerequisites:** .NET 10 SDK, API keys for gg.deals and ITAD.

Create `src/SteamDealX.Api/appsettings.Development.json`:

```json
{
  "GgDeals": { "ApiKey": "your-gg-deals-key" },
  "Itad": { "ApiKey": "your-itad-key" },
  "AllowedOrigins": "*"
}
```

```bash
dotnet restore
dotnet run --project src/SteamDealX.Api/SteamDealX.Api.csproj
```

API available at `https://localhost:5001`. Docs at `https://localhost:5001/scalar`.

```bash
# Run tests
dotnet test tests/SteamDealX.Clients.Tests/
```

## Deployment

The app is deployed to [Fly.io](https://fly.io) (`gru` region, São Paulo) with a persistent volume for the SQLite cache.

```bash
fly deploy
```

Key environment variables:

| Variable | Description |
|----------|-------------|
| `GgDeals__ApiKey` | gg.deals API key |
| `Itad__ApiKey` | IsThereAnyDeal API key |
| `Cache__DbPath` | SQLite cache file path (default: `./dealscache.db`) |
| `AllowedOrigins` | CORS origins (comma-separated) |

## Error Responses

All errors follow [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457). Machine-readable codes: `GAME_NOT_FOUND`, `VALIDATION_ERROR`, `UPSTREAM_ERROR`, `UPSTREAM_RATE_LIMITED`, `RATE_LIMITED`, `INTERNAL_ERROR`.

## Attribution

gg.deals links in responses must be displayed as active hyperlinks per their Terms of Service.
