# FinDistill.BI — User Documentation

> Automated BI-system for collecting, storing, and visualizing market quotes (stocks, ETFs, cryptocurrencies)
> with a three-tier data architecture: **Data Lake → DWH → Data Marts**.

---

## Table of Contents

1. [Functional Overview](#1-functional-overview)
2. [Architecture & Design Patterns](#2-architecture--design-patterns)
3. [Technology Stack](#3-technology-stack)
4. [Project Structure](#4-project-structure)
5. [Data Architecture (3-Tier)](#5-data-architecture-3-tier)
6. [ETL Pipeline](#6-etl-pipeline)
7. [Configuration Reference](#7-configuration-reference)
8. [Running the Application](#8-running-the-application)
9. [Testing Strategy](#9-testing-strategy)
10. [CI/CD Pipeline](#10-cicd-pipeline)
11. [Simplifications (Educational/Demo Scope)](#11-simplifications-educationaldemo-scope)
12. [Production Hardening Roadmap](#12-production-hardening-roadmap)

---

## 1. Functional Overview

### What the system does

FinDistill.BI automatically fetches market data from external APIs, processes it through a three-stage ETL pipeline, stores it in a star-schema data warehouse, and presents it via a web dashboard with interactive charts.

### Key capabilities

| Capability | Description |
|---|---|
| **Multi-source ingestion** | Yahoo Finance (stocks/ETFs) and CoinGecko (crypto) via Strategy pattern |
| **Three-tier data architecture** | Raw JSON → Star Schema DWH → Pre-aggregated Data Marts |
| **Scheduled ETL** | Autonomous Worker Service runs Extract→Transform→Load on a configurable interval |
| **Manual sync** | "Sync Now" button in the web dashboard triggers ETL on demand |
| **Interactive dashboard** | Portfolio summary, daily performance table, per-asset Chart.js price charts |
| **Multi-DBMS support** | SQL Server and PostgreSQL switchable via single config setting |
| **Optional OLAP** | ClickHouse integration for analytical Data Mart queries (feature flag) |
| **Optional caching** | Redis cache layer for Data Mart reads (feature flag, interface ready) |

### User-facing screens

- **Dashboard** (`/Dashboard/Index`) — portfolio summary table with last price, daily change %, and links to individual asset charts.
- **Asset Detail** (`/Asset/Detail?ticker=AAPL`) — Chart.js candlestick-style line chart + OHLCV history table for a single asset over configurable number of days.

---

## 2. Architecture & Design Patterns

### 2.1 Clean Architecture (4 layers)

The solution strictly follows Clean Architecture with unidirectional dependency flow:

```
Domain  ←  Application  ←  Infrastructure  ←  Web / Worker
(no deps)  (→ Domain)     (→ Domain, App)    (→ App, Infra)
```

- **Domain** — entities, interfaces, enums, value objects. Zero infrastructure dependencies.
- **Application** — ETL services, dashboard service, DTOs. Business logic lives here.
- **Infrastructure** — EF Core, Dapper, HTTP clients, DI registration. All I/O.
- **Web** — ASP.NET Core MVC controllers, Razor views, ViewModels.
- **Worker** — .NET `BackgroundService` hosting the ETL scheduler.

### 2.2 Design Patterns Used

| Pattern | Where | Purpose |
|---|---|---|
| **Strategy** | `IMarketDataProvider` → `YahooFinanceProvider`, `CoinGeckoProvider` | Swap API sources without changing ETL logic |
| **Repository** | `IDimAssetRepository` → `DimAssetRepository` (EF Core) | Abstract data access; write via EF Core, read via Dapper |
| **CQRS-lite** | EF Core for writes, Dapper/ClickHouse for reads | Optimize read/write paths independently |
| **Result Pattern** | `Result<T>`, `Result`, `Error` | Explicit error handling without exceptions in control flow |
| **Options Pattern** | `IOptions<DatabaseOptions>`, `IOptions<FeaturesOptions>`, etc. | Strongly-typed configuration binding |
| **Feature Flags** | `FeaturesOptions.UseRedis`, `UseClickHouse` | Toggle optional infrastructure without code changes |
| **Null Object** | `NullCacheService` | Transparent no-op when Redis is disabled |
| **Delegating Handler** | `RetryDelegatingHandler` | Cross-cutting HTTP retry with exponential backoff |
| **Factory** | `DapperConnectionFactory` | Create provider-specific `IDbConnection` (SqlConnection/NpgsqlConnection) |

### 2.3 Result Pattern

ETL services and dashboard methods return `Result` / `Result<T>` instead of throwing exceptions:

```csharp
// Application layer
public async Task<Result> ExtractAsync(CancellationToken ct) { ... }

// Caller
var result = await _extractor.ExtractAsync(ct);
if (result.IsFailure)
    _logger.LogError("Extract failed: {Error}", result.Error.Message);
```

Error codes include `Etl.ExtractPartialFailure`, `Etl.TransformFailure`, `Etl.LoadFailure`, `Dashboard.ReadFailure`.

---

## 3. Technology Stack

| Category | Technology | Version |
|---|---|---|
| Platform | .NET | 8.0 |
| Language | C# | 12 |
| Web framework | ASP.NET Core MVC | 8.0 |
| ORM (writes) | Entity Framework Core | 8.0.16 |
| Micro-ORM (reads) | Dapper | 2.1.66 |
| RDBMS | SQL Server / PostgreSQL | Configurable |
| OLAP (optional) | ClickHouse | Via `ClickHouse.Client` 7.8.0 |
| Caching (optional) | Redis | Interface ready (`ICacheService`) |
| Logging | Serilog | Console + rolling file sinks |
| Charts | Chart.js | v4 (CDN) |
| Testing | xUnit + Moq + Testcontainers | Unit + Integration |

---

## 4. Project Structure

```
FinDistill.BI.sln
│
├── src/
│   ├── FinDistill.Domain/           — Entities, Interfaces, Enums, Models, Common (Result/Error)
│   ├── FinDistill.Application/      — ETL Services, DashboardService, DTOs, Interfaces
│   ├── FinDistill.Infrastructure/   — EF Core, Dapper, API Clients, DI, Caching, HTTP
│   ├── FinDistill.Web/              — MVC Controllers, Views, ViewModels, Program.cs
│   └── FinDistill.Worker/           — BackgroundService (EtlWorker), Program.cs
│
├── tests/
│   ├── FinDistill.Domain.Tests/
│   ├── FinDistill.Application.Tests/
│   └── FinDistill.Infrastructure.Tests/  — Unit + Integration (Testcontainers)
│
└── Scripts/
    ├── DataMartViews_SqlServer.sql
    ├── DataMartViews_PostgreSQL.sql
    └── ClickHouse_DDL.sql
```

---

## 5. Data Architecture (3-Tier)

### 5.1 Data Lake (schema `lake`)

Raw JSON responses stored as-is for auditing and reprocessing:

| Table | Key columns |
|---|---|
| `lake.RawIngestData` | `Id`, `Source`, `Endpoint`, `RawContent`, `LoadedAt`, `IsProcessed` |

### 5.2 Data Warehouse — Star Schema (schema `dwh`)

**Dimensions:**

| Table | Purpose | Key |
|---|---|---|
| `dwh.DimAssets` | Tracked financial instruments | `AssetKey` (surrogate), unique `Ticker` |
| `dwh.DimDates` | Calendar dimension | `DateKey` (YYYYMMDD), auto-populated fields |
| `dwh.DimSources` | API data sources | `SourceKey` (surrogate), unique `SourceName` |

**Facts:**

| Table | Purpose | Unique constraint |
|---|---|---|
| `dwh.FactQuotes` | OHLCV price data | `(AssetKey, DateKey, SourceKey)` |

### 5.3 Data Marts (schema `mart`)

Pre-aggregated SQL Views for dashboard consumption:

| View | Description |
|---|---|
| `mart.v_DailyPerformance` | Latest close price and daily change % per asset |
| `mart.v_AssetHistory` | Historical OHLCV data for chart rendering |
| `mart.v_PortfolioSummary` | Last close, previous close, change % across all assets |

**Read path:** Web → `DashboardService` → `IDataMartReader` → Dapper (or ClickHouse) → SQL Views  
**Write path:** Worker → ETL Services → Repositories (EF Core) → DWH tables

---

## 6. ETL Pipeline

### Pipeline stages

```
Extract ──→ Transform ──→ Load ──→ [ClickHouse Sync]
  │              │            │            │
  ▼              ▼            ▼            ▼
API Sources   Parse JSON    DWH Write    Optional
→ Data Lake   → Validate   → Upsert     OLAP sync
              → Normalize    Dimensions
                           → Insert Facts
                           → Mark Processed
```

### Extract (`ExtractorService`)
- Iterates all registered `IMarketDataProvider` implementations
- Fetches tickers from `ITickerProvider` (configured in `DataSources` section)
- Stores raw JSON in `lake.RawIngestData`
- Inter-request throttling via `RequestDelayMs` to avoid API rate limits

### Transform (`TransformerService`)
- Reads unprocessed records from Data Lake (`IsProcessed = false`)
- Parses JSON, validates data, normalizes to `ParsedQuoteDto`
- Supports both Yahoo Finance and CoinGecko JSON formats

### Load (`LoaderService`)
- Upserts dimension records (`DimAsset`, `DimDate`, `DimSource`) with in-memory caching per batch
- Intra-batch deduplication via `HashSet<(AssetKey, DateKey, SourceKey)>`
- DB-level duplicate check via `ExistsAsync`
- Batch `AddRangeAsync` for all new facts
- Marks source Data Lake records as processed

### Orchestrator (`EtlOrchestrator`)
- Chains E→T→L stages with Result pattern
- Logs timing, record counts, errors
- Optionally calls `IClickHouseSyncService.SyncAsync` after Load

### Scheduling (`EtlWorker`)
- .NET `BackgroundService` with configurable interval (`EtlSchedule:IntervalMinutes`)
- Creates scoped DI container per run
- Graceful shutdown via `CancellationToken`

---

## 7. Configuration Reference

### `appsettings.json` structure

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FinDistillBI;...",
    "ClickHouse": null       // Required only when UseClickHouse = true
  },
  "Database": {
    "Provider": "SqlServer"  // "SqlServer" | "PostgreSQL"
  },
  "Features": {
    "UseRedis": false,       // Enable Redis caching (Phase 10)
    "UseClickHouse": false   // Enable ClickHouse for Data Marts
  },
  "EtlSchedule": {
    "IntervalMinutes": 15    // Worker only
  },
  "DataSources": {
    "YahooFinance": {
      "Enabled": true,
      "Tickers": ["AAPL", "MSFT", "SPY", "QQQ"],
      "RequestDelayMs": 1000
    },
    "CoinGecko": {
      "Enabled": true,
      "CoinIds": ["bitcoin", "ethereum"],
      "VsCurrency": "usd",
      "ApiKey": null,        // CoinGecko Demo API key (required for free tier)
      "RequestDelayMs": 1500
    }
  }
}
```

### Secrets management

API keys should be stored in `appsettings.Development.json` (excluded from git via `.gitignore`) or via User Secrets / environment variables. Never commit API keys to `appsettings.json`.

---

## 8. Running the Application

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB or full instance) **or** PostgreSQL
- (Optional) CoinGecko Demo API key from [coingecko.com/developers/dashboard](https://www.coingecko.com/en/developers/dashboard)

### Setup

```bash
# 1. Restore packages
dotnet restore

# 2. Apply EF Core migrations
dotnet ef database update --project src/FinDistill.Web

# 3. Configure CoinGecko API key (in appsettings.Development.json)
# "DataSources": { "CoinGecko": { "ApiKey": "CG-your_key_here" } }

# 4. Run the Worker (ETL)
dotnet run --project src/FinDistill.Worker

# 5. Run the Web dashboard (separate terminal)
dotnet run --project src/FinDistill.Web
```

### Switching databases

Change `Database:Provider` to `"PostgreSQL"` and update `ConnectionStrings:DefaultConnection` accordingly. Both EF Core and Dapper connection factories respect this setting.

---

## 9. Testing Strategy

### Unit tests (73 tests)

| Project | Coverage |
|---|---|
| `FinDistill.Domain.Tests` | Result/Error types, entity validation |
| `FinDistill.Application.Tests` | All ETL services, DashboardService, EtlOrchestrator |
| `FinDistill.Infrastructure.Tests` | RetryDelegatingHandler, DateOnlyTypeHandler |

All services tested via Moq with `Result.IsSuccess` / `IsFailure` assertions.

### Integration tests (20 tests, require Docker)

| Test class | Validates |
|---|---|
| `DimAssetRepositoryIntegrationTests` | Upsert insert→update, GetByTickerAsync |
| `DimDateRepositoryIntegrationTests` | Idempotent EnsureDateExistsAsync, weekend detection |
| `FactQuoteRepositoryIntegrationTests` | AddRangeAsync + ExistsAsync |
| `RawIngestDataRepositoryIntegrationTests` | AddRangeAsync + GetUnprocessedAsync + MarkAsProcessedAsync |
| `DapperDataMartReaderIntegrationTests` | SQL Views via Dapper with seeded data |
| `MigrationIntegrationTests` | No pending migrations, schema existence |

Uses **Testcontainers** (SQL Server + PostgreSQL) with `[DockerAvailableFact]` — tests automatically skip when Docker is unavailable.

---

## 10. CI/CD Pipeline

Automated via GitHub Actions (`.github/workflows/ci-cd.yml`).

### Triggers

| Event | Scope |
|---|---|
| `push` to `main` | Full pipeline: build → test → publish artifacts |
| `pull_request` to `main` | Build + test only (no publish) |

### Pipeline stages

```
Restore → Build (Release) → Unit Tests → Integration Tests → Publish → Upload Artifacts
                                                │                        │
                                          Testcontainers           Only on push to main
                                          (continue-on-error)
```

### Unit tests

Run for all three test projects. Failures **block** the pipeline:

```bash
dotnet test --filter "FullyQualifiedName!~IntegrationTests"
```

### Integration tests

Use **Testcontainers** — each test fixture starts its own SQL Server / PostgreSQL container via Docker. Marked with `continue-on-error: true` since they depend on Docker daemon availability:

```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### Artifacts

| Artifact | Published when | Retention |
|---|---|---|
| `test-results` (TRX) | Always | 30 days |
| `findistill-web` | Push to `main` | 14 days |
| `findistill-worker` | Push to `main` | 14 days |

---

## 11. Simplifications (Educational/Demo Scope)

The following design decisions were made deliberately to keep the project focused on demonstrating architectural patterns rather than production-readiness:

### 11.1 Security

| Simplification | Production expectation |
|---|---|
| No authentication/authorization | ASP.NET Core Identity or OAuth2/OIDC |
| No HTTPS enforcement in dev | HSTS + TLS certificates |
| CSRF protection on Sync only | Consistent anti-forgery across all mutations |
| API keys in `appsettings.Development.json` | Azure Key Vault / AWS Secrets Manager |

### 11.2 Data processing

| Simplification | Production expectation |
|---|---|
| Full-table sync for ClickHouse (TRUNCATE + re-insert) | Incremental/CDC sync |
| Single-threaded ETL (sequential tickers) | Parallel extraction with `SemaphoreSlim` |
| In-memory deduplication (per-batch HashSet) | Upsert/MERGE at database level |
| No data retention policy | TTL-based purging of Data Lake records |
| `SaveChangesAsync` per dimension upsert | Bulk upsert via `ExecuteUpdateAsync` batching |

### 11.3 Infrastructure

| Simplification | Production expectation |
|---|---|
| Rolling file logs only | Centralized logging (ELK, Seq, Application Insights) |
| No health checks | `/health` endpoint with DB/API liveness probes |
| No containerization | Dockerfile + docker-compose |
| No telemetry/metrics | OpenTelemetry + Prometheus/Grafana |

### 11.4 API resilience

| Simplification | Production expectation |
|---|---|
| Custom `RetryDelegatingHandler` (3 retries, exponential backoff) | Polly policies (retry + circuit breaker + timeout) |
| No circuit breaker pattern | Polly `CircuitBreakerPolicy` per provider |
| Static 1–1.5s inter-request delay | Adaptive rate limiting based on API response headers |
| Yahoo Finance v8 undocumented API | Official API with SLA or paid data provider |

### 11.5 Frontend

| Simplification | Production expectation |
|---|---|
| Server-side rendered views (MVC + Razor) | SPA (React/Vue) or Blazor for rich interactivity |
| Chart.js via CDN | Bundled/versioned JS assets (Webpack/Vite) |
| No real-time updates | SignalR for live price updates |
| No responsive design optimization | Tailwind CSS / Bootstrap 5 responsive grid |

---

## 12. Production Hardening Roadmap

### Priority 1 — Security & reliability

- [ ] Add ASP.NET Core Identity with role-based authorization
- [ ] Move secrets to Azure Key Vault / environment variables
- [ ] Add Polly policies (circuit breaker, bulkhead, timeout) to replace custom retry handler
- [ ] Add health check endpoints (`/health`, `/ready`) for load balancer probes
- [ ] Implement structured exception middleware with ProblemDetails responses

### Priority 2 — Observability

- [ ] Integrate OpenTelemetry (traces, metrics, logs)
- [ ] Add Prometheus metrics endpoint for ETL duration, record counts, API error rates
- [ ] Replace file sinks with centralized logging (Seq, ELK, or Application Insights)
- [ ] Add correlation IDs across ETL pipeline stages

### Priority 3 — Data pipeline improvements

- [ ] Parallel extraction with configurable concurrency (`SemaphoreSlim`)
- [ ] Incremental ClickHouse sync (delta-only inserts by `LoadedAt` watermark)
- [ ] Data Lake retention policy (archive/purge records older than N days)
- [ ] Database-level upsert (MERGE/ON CONFLICT) to replace read-then-write pattern
- [ ] Dead-letter queue for failed transformations

### Priority 4 — Infrastructure & deployment

- [ ] Dockerfile for Web and Worker services
- [ ] `docker-compose.yml` with SQL Server, Redis, ClickHouse
- [x] GitHub Actions CI (build → test → publish artifacts)
- [ ] Kubernetes Helm chart (optional)
- [ ] Blue-green or canary deployment strategy

### Priority 5 — Feature completeness

- [ ] **Phase 10**: Redis caching (`RedisCacheService` with cache-aside in `DashboardService`)
- [ ] Additional data providers (Alpha Vantage, Polygon.io)
- [ ] User-configurable watchlists (database-backed)
- [ ] Real-time price updates via SignalR
- [ ] Export to CSV/Excel
- [ ] Alert system (price threshold notifications via email/Telegram)

---

*Generated from the FinDistill.BI codebase. For architectural rules, see [`.github/copilot-instructions.md`](.github/copilot-instructions.md). For implementation progress, see [`.github/WORKPLAN.md`](.github/WORKPLAN.md).*
