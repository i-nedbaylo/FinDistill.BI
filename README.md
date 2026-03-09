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
8. [Optional Configuration Guide](#8-optional-configuration-guide)
9. [Running the Application](#9-running-the-application)
10. [Testing Strategy](#10-testing-strategy)
11. [CI/CD Pipeline](#11-cicd-pipeline)
12. [Deployment](#12-deployment)
13. [Simplifications (Educational/Demo Scope)](#13-simplifications-educationaldemo-scope)
14. [Production Hardening Roadmap](#14-production-hardening-roadmap)

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

## 8. Optional Configuration Guide

This section provides step-by-step instructions for every optional setting in the application. All options use the **Options Pattern** (`IOptions<T>`) and can be set via `appsettings.json`, `appsettings.{Environment}.json`, environment variables, or User Secrets.

> **Environment variable naming:** Replace `:` with `__` (double underscore). Arrays use indexed notation: `Tickers__0`, `Tickers__1`, etc.

---

### 8.1 Switch Database Provider (SQL Server ↔ PostgreSQL)

**Options class:** `DatabaseOptions` — section `"Database"`

The application supports both SQL Server and PostgreSQL. All layers (EF Core, Dapper, Data Mart readers) respect this single setting.

**Step 1:** Change the provider in `appsettings.json`:

```jsonc
{
  "Database": {
    "Provider": "PostgreSQL"  // "SqlServer" (default) or "PostgreSQL"
  }
}
```

**Step 2:** Update the connection string:

```jsonc
{
  "ConnectionStrings": {
    // SQL Server
    "DefaultConnection": "Server=localhost;Database=FinDistillBI;Trusted_Connection=True;TrustServerCertificate=True;"

    // PostgreSQL
    "DefaultConnection": "Host=localhost;Port=5432;Database=findistill;Username=postgres;Password=your_password;"
  }
}
```

**Step 3:** Apply EF Core migrations for the chosen provider:

```bash
dotnet ef database update --project src/FinDistill.Web
```

**Via environment variables:**

```bash
Database__Provider=PostgreSQL
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=findistill;..."
```

---

### 8.2 Auto-Migrate on Startup

**Options class:** `DatabaseOptions` — property `AutoMigrate`

When enabled, EF Core migrations are applied automatically when the Web application starts. Useful for cloud deployments where you cannot run `dotnet ef` manually.

```jsonc
{
  "Database": {
    "Provider": "PostgreSQL",
    "AutoMigrate": true  // default: false (safe-by-default)
  }
}
```

> ⚠️ **Warning:** Do not enable in multi-instance deployments — concurrent migrations can corrupt the database. Use only for single-instance deployments (Railway, Render, local Docker).

**Via environment variable:**

```bash
Database__AutoMigrate=true
```

---

### 8.3 Configure Data Sources

**Options class:** `DataSourcesOptions` — section `"DataSources"`

#### 8.3.1 Yahoo Finance

```jsonc
{
  "DataSources": {
    "YahooFinance": {
      "Enabled": true,                              // Enable/disable this source
      "Tickers": ["AAPL", "MSFT", "GOOGL", "TSLA"], // Stock/ETF ticker symbols
      "RequestDelayMs": 1000                         // Delay between requests (ms) to avoid HTTP 429
    }
  }
}
```

To add more tickers, simply extend the array. To disable Yahoo Finance entirely, set `Enabled` to `false`.

**Via environment variables (indexed notation for arrays):**

```bash
DataSources__YahooFinance__Enabled=true
DataSources__YahooFinance__Tickers__0=AAPL
DataSources__YahooFinance__Tickers__1=MSFT
DataSources__YahooFinance__Tickers__2=GOOGL
DataSources__YahooFinance__RequestDelayMs=1000
```

#### 8.3.2 CoinGecko

```jsonc
{
  "DataSources": {
    "CoinGecko": {
      "Enabled": true,                    // Enable/disable this source
      "CoinIds": ["bitcoin", "ethereum", "solana"],  // CoinGecko coin identifiers
      "VsCurrency": "usd",                // Quote currency (usd, eur, etc.)
      "ApiKey": null,                     // Demo API key (see below)
      "RequestDelayMs": 1500              // Delay between requests (ms)
    }
  }
}
```

**Getting a CoinGecko API key (recommended):**

1. Register at [coingecko.com/developers/dashboard](https://www.coingecko.com/en/developers/dashboard)
2. Create a **Demo API key** (free, rate-limited)
3. Store it securely — **never commit to git**:

```bash
# Option A: User Secrets (development)
dotnet user-secrets set "DataSources:CoinGecko:ApiKey" "CG-your_key_here" --project src/FinDistill.Web
dotnet user-secrets set "DataSources:CoinGecko:ApiKey" "CG-your_key_here" --project src/FinDistill.Worker

# Option B: appsettings.Development.json (excluded via .gitignore)
# { "DataSources": { "CoinGecko": { "ApiKey": "CG-your_key_here" } } }

# Option C: Environment variable
DataSources__CoinGecko__ApiKey=CG-your_key_here
```

**Via environment variables (indexed notation for arrays):**

```bash
DataSources__CoinGecko__Enabled=true
DataSources__CoinGecko__CoinIds__0=bitcoin
DataSources__CoinGecko__CoinIds__1=ethereum
DataSources__CoinGecko__CoinIds__2=solana
DataSources__CoinGecko__VsCurrency=usd
```

---

### 8.4 Configure ETL Schedule

**Options class:** `EtlScheduleOptions` — section `"EtlSchedule"`

Controls how often the ETL pipeline runs (in both the dedicated Worker service and the in-process ETL worker).

```jsonc
{
  "EtlSchedule": {
    "IntervalMinutes": 30,   // Run ETL every 30 minutes (default: 15)
    "CronExpression": null   // Reserved for future use
  }
}
```

**Via environment variable:**

```bash
EtlSchedule__IntervalMinutes=30
```

---

### 8.5 Enable In-Process ETL Worker

**Options class:** `FeaturesOptions` — property `RunEtlInProcess`

Runs the ETL pipeline inside the Web process as a `BackgroundService`. This eliminates the need for the separate Worker service — useful for free hosting tiers (Render.com) that only allow a single process.

```jsonc
{
  "Features": {
    "RunEtlInProcess": true  // default: false
  }
}
```

**Deployment modes:**

| `RunEtlInProcess` | Web | Worker | Use case |
|---|---|---|---|
| `false` (default) | HTTP only | ETL only | Production: two separate processes |
| `true` | HTTP + ETL | Not needed | Demo/free tier: single process |

When `RunEtlInProcess` is `true`, the `EtlSchedule` section must also be present in the Web project's `appsettings.json` (already included by default).

**Via environment variable:**

```bash
Features__RunEtlInProcess=true
```

---

### 8.6 Enable ClickHouse for Data Marts (Optional)

**Options class:** `FeaturesOptions` — property `UseClickHouse`

Replaces Dapper (SQL Server/PostgreSQL) with ClickHouse as the read engine for Data Mart queries. ClickHouse provides superior analytical query performance for large datasets.

**Step 1:** Set up a ClickHouse instance and create tables:

```bash
# Apply the DDL script to your ClickHouse instance
clickhouse-client --multiquery < Scripts/ClickHouse_DDL.sql
```

**Step 2:** Configure the connection:

```jsonc
{
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=8123;Database=default;"
  },
  "Features": {
    "UseClickHouse": true  // default: false
  }
}
```

**What changes when enabled:**

| Component | `UseClickHouse = false` | `UseClickHouse = true` |
|---|---|---|
| `IDataMartReader` | `DapperDataMartReader` | `ClickHouseDataMartReader` |
| `IClickHouseSyncService` | Not registered | `ClickHouseSyncService` |
| ETL pipeline | E → T → L | E → T → L → ClickHouse Sync |

**Via environment variables:**

```bash
Features__UseClickHouse=true
ConnectionStrings__ClickHouse="Host=clickhouse-server;Port=8123;Database=default;"
```

---

### 8.7 Enable Redis Caching (Optional — Interface Ready)

**Options class:** `FeaturesOptions` — property `UseRedis`

The `ICacheService` interface and `NullCacheService` (no-op) are already implemented. Redis integration (`RedisCacheService`) is planned for Phase 10.

```jsonc
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"  // Required when UseRedis = true
  },
  "Features": {
    "UseRedis": true  // default: false
  }
}
```

| `UseRedis` | `ICacheService` implementation | Behavior |
|---|---|---|
| `false` (default) | `NullCacheService` | All cache calls return `null` — transparent pass-through |
| `true` | `RedisCacheService` (Phase 10) | Cache-aside pattern for Data Mart reads |

---

### 8.8 Configure Serilog Logging

**Serilog** is configured via the `"Serilog"` section in `appsettings.json`. The application uses structured logging with Console and rolling File sinks.

**Development (Console + File):**

```jsonc
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    }
  }
}
```

**Production / Container (Console only):**

File logging is automatically disabled in the `Production` environment to avoid issues with ephemeral/read-only container filesystems.

```jsonc
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ]
  }
}
```

---

### 8.9 Quick Reference — All Configuration Sections

| Section | Options Class | Key Properties | Default |
|---|---|---|---|
| `Database` | `DatabaseOptions` | `Provider`, `AutoMigrate` | `"SqlServer"`, `false` |
| `Features` | `FeaturesOptions` | `UseRedis`, `UseClickHouse`, `RunEtlInProcess` | all `false` |
| `EtlSchedule` | `EtlScheduleOptions` | `IntervalMinutes`, `CronExpression` | `15`, `null` |
| `DataSources:YahooFinance` | `YahooFinanceOptions` | `Enabled`, `Tickers`, `RequestDelayMs` | `true`, `[]`, `1000` |
| `DataSources:CoinGecko` | `CoinGeckoOptions` | `Enabled`, `CoinIds`, `VsCurrency`, `ApiKey`, `RequestDelayMs` | `true`, `[]`, `"usd"`, `null`, `1500` |
| `ConnectionStrings` | — | `DefaultConnection`, `ClickHouse`, `Redis` | — |
| `Serilog` | — | `MinimumLevel`, `WriteTo` | `Information`, Console + File |

---

### 8.10 Example: Minimal Configuration for Render.com (Free Tier)

Single-process deployment with PostgreSQL, in-process ETL, and no optional features:

```jsonc
// appsettings.Production.json (or environment variables)
{
  "Database": {
    "Provider": "PostgreSQL",
    "AutoMigrate": true
  },
  "Features": {
    "UseRedis": false,
    "UseClickHouse": false,
    "RunEtlInProcess": true
  },
  "EtlSchedule": {
    "IntervalMinutes": 15
  },
  "DataSources": {
    "YahooFinance": {
      "Enabled": true,
      "Tickers": ["AAPL", "MSFT"]
    },
    "CoinGecko": {
      "Enabled": true,
      "CoinIds": ["bitcoin"],
      "VsCurrency": "usd"
    }
  }
}
```

Equivalent environment variables:

```bash
Database__Provider=PostgreSQL
Database__AutoMigrate=true
Features__RunEtlInProcess=true
EtlSchedule__IntervalMinutes=15
DataSources__YahooFinance__Enabled=true
DataSources__YahooFinance__Tickers__0=AAPL
DataSources__YahooFinance__Tickers__1=MSFT
DataSources__CoinGecko__Enabled=true
DataSources__CoinGecko__CoinIds__0=bitcoin
DataSources__CoinGecko__VsCurrency=usd
```

---

## 9. Running the Application

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

## 10. Testing Strategy

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

## 11. CI/CD Pipeline

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
dotnen
