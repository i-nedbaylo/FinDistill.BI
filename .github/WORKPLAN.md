# FinDistill.BI — План работ (Work Plan)

> Этот файл — чек-лист реализации проекта. Обновляется по мере выполнения шагов.
> Copilot **ДОЛЖЕН** сверяться с этим файлом при каждом продолжении работы.

---

## Условные обозначения

- [ ] — не начато
- [🔄] — в процессе
- [✅] — завершено

---

## Фаза 0. Создание структуры решения (Solution Scaffolding)

- [✅] **0.1** Создать solution файл `FinDistill.BI.sln`
- [✅] **0.2** Создать проект `src/FinDistill.Domain` (Class Library, net8.0)
- [✅] **0.3** Создать проект `src/FinDistill.Application` (Class Library, net8.0)
- [✅] **0.4** Создать проект `src/FinDistill.Infrastructure` (Class Library, net8.0)
- [✅] **0.5** Создать проект `src/FinDistill.Web` (ASP.NET Core MVC, net8.0)
- [✅] **0.6** Создать проект `src/FinDistill.Worker` (Worker Service, net8.0)
- [✅] **0.7** Создать тестовые проекты: `tests/FinDistill.Domain.Tests`, `tests/FinDistill.Application.Tests`, `tests/FinDistill.Infrastructure.Tests` (xUnit, net8.0)
- [✅] **0.8** Добавить все проекты в solution
- [✅] **0.9** Настроить межпроектные ссылки (ProjectReference) согласно архитектуре:
  - Application → Domain
  - Infrastructure → Domain, Application
  - Web → Application, Infrastructure
  - Worker → Application, Infrastructure
  - Domain.Tests → Domain
  - Application.Tests → Application, Domain
  - Infrastructure.Tests → Infrastructure, Application, Domain
- [✅] **0.10** Включить `<Nullable>enable</Nullable>` во всех проектах
- [✅] **0.11** Убедиться, что solution собирается (`dotnet build`)

---

## Фаза 1. Domain Layer (FinDistill.Domain)

- [✅] **1.1** Создать Enums:
  - `Enums/AssetType.cs` — Stock, ETF, Crypto
  - `Enums/DataSourceType.cs` — YahooFinance, CoinGecko
- [✅] **1.2** Создать Entities (Data Lake):
  - `Entities/RawIngestData.cs` — Id, Source, Endpoint, RawContent, LoadedAt, IsProcessed
- [✅] **1.3** Создать Entities (DWH — Dimensions):
  - `Entities/DimAsset.cs` — AssetKey, Ticker, Name, AssetType, Exchange, IsActive, CreatedAt, UpdatedAt
  - `Entities/DimDate.cs` — DateKey, FullDate, Year, Quarter, Month, Day, DayOfWeek, WeekOfYear, IsWeekend
  - `Entities/DimSource.cs` — SourceKey, SourceName, BaseUrl, IsActive
- [✅] **1.4** Создать Entities (DWH — Facts):
  - `Entities/FactQuote.cs` — Id, AssetKey, DateKey, SourceKey, OpenPrice, HighPrice, LowPrice, ClosePrice, Volume, LoadedAt + навигационные свойства
- [✅] **1.5** Создать интерфейсы репозиториев:
  - `Interfaces/IRawIngestDataRepository.cs` — AddAsync, GetUnprocessedAsync, MarkAsProcessedAsync
  - `Interfaces/IDimAssetRepository.cs` — GetByTickerAsync, UpsertAsync, GetAllActiveAsync
  - `Interfaces/IDimDateRepository.cs` — EnsureDateExistsAsync, GetByDateAsync
  - `Interfaces/IDimSourceRepository.cs` — GetByNameAsync, UpsertAsync
  - `Interfaces/IFactQuoteRepository.cs` — AddRangeAsync, ExistsAsync (по AssetKey+DateKey+SourceKey)
- [✅] **1.6** Создать интерфейс IMarketDataProvider (Strategy):
  - `Interfaces/IMarketDataProvider.cs` — SourceType, FetchRawDataAsync, FetchBulkDataAsync
- [✅] **1.7** Создать интерфейс IDataMartReader (Dapper/ClickHouse-чтение):
  - `Interfaces/IDataMartReader.cs` — GetDailyPerformanceAsync, GetAssetHistoryAsync, GetPortfolioSummaryAsync
  - `Models/DailyPerformanceRecord.cs`, `Models/AssetHistoryRecord.cs`, `Models/PortfolioSummaryRecord.cs` — read models для Data Marts
  - ⚡ Ключевая точка расширения: реализация переключается между Dapper и ClickHouse через конфигурацию
- [✅] **1.8** Создать интерфейс ICacheService (точка расширения для Redis):
  - `Interfaces/ICacheService.cs` — GetAsync<T>, SetAsync<T>, RemoveAsync
  - ⚡ Создаётся сразу, реализация — в Фазе 10 (NullCacheService + RedisCacheService)
- [✅] **1.9** Собрать проект, убедиться что нет ошибок

---

## Фаза 2. Application Layer (FinDistill.Application)

- [✅] **2.1** Создать DTO:
  - `DTOs/ParsedQuoteDto.cs` — Ticker, Date, Open, High, Low, Close, Volume, SourceType
  - `DTOs/DailyPerformanceDto.cs` — Ticker, Name, AssetType, ClosePrice, ChangePercent
  - `DTOs/AssetHistoryDto.cs` — Date, Open, High, Low, Close, Volume
  - `DTOs/PortfolioSummaryDto.cs` — Ticker, Name, AssetType, LastClose, PreviousClose, ChangePercent
- [✅] **2.2** Создать интерфейсы ETL-сервисов:
  - `Interfaces/IExtractorService.cs` — ExtractAsync(CancellationToken)
  - `Interfaces/ITransformerService.cs` — TransformAsync(CancellationToken)
  - `Interfaces/ILoaderService.cs` — LoadAsync(IEnumerable<ParsedQuoteDto>, CancellationToken)
  - `Interfaces/IEtlOrchestrator.cs` — RunEtlPipelineAsync(CancellationToken)
- [✅] **2.3** Создать интерфейс IDashboardService:
  - `Interfaces/IDashboardService.cs` — GetDailyPerformanceAsync, GetAssetHistoryAsync, GetPortfolioSummaryAsync
- [✅] **2.4** Реализовать ETL-сервисы:
  - `Services/ExtractorService.cs` — использует IEnumerable<IMarketDataProvider> + IRawIngestDataRepository
  - `Services/TransformerService.cs` — читает Lake, парсит JSON, валидирует, возвращает ParsedQuoteDto
  - `Services/LoaderService.cs` — записывает в DWH через репозитории, помечает Lake как обработанный
  - `Services/EtlOrchestrator.cs` — оркестрирует E→T→L, ловит исключения, логирует
- [✅] **2.5** Реализовать DashboardService:
  - `Services/DashboardService.cs` — делегирует чтение в IDataMartReader, оборачивает ICacheService
- [✅] **2.6** Собрать проект, убедиться что нет ошибок

---

## Фаза 3. Infrastructure Layer — Часть 1: Database (FinDistill.Infrastructure)

- [✅] **3.1** Добавить NuGet-пакеты:
  - `Microsoft.EntityFrameworkCore` 8.0.16
  - `Microsoft.EntityFrameworkCore.SqlServer` 8.0.16
  - `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.11
  - `Dapper` 2.1.66
  - `Microsoft.Data.SqlClient` 5.2.2 (для Dapper + SQL Server)
  - `Npgsql` 8.0.6 (для Dapper + PostgreSQL)
- [✅] **3.2** Создать класс настроек `Configuration/DatabaseOptions.cs`:
  - `Provider` — "SqlServer" | "PostgreSQL"
  - (connection string берётся из ConnectionStrings:DefaultConnection)
- [✅] **3.3** Создать `Persistence/FinDistillDbContext.cs`:
  - DbSet для: RawIngestData, DimAsset, DimDate, DimSource, FactQuote
  - OnModelCreating: ApplyConfigurationsFromAssembly
- [✅] **3.4** Создать Fluent-конфигурации EF Core (EntityTypeConfiguration):
  - `Persistence/Configurations/RawIngestDataConfiguration.cs` — schema lake, filtered index
  - `Persistence/Configurations/DimAssetConfiguration.cs` — schema dwh, unique Ticker
  - `Persistence/Configurations/DimDateConfiguration.cs` — schema dwh, ValueGeneratedNever
  - `Persistence/Configurations/DimSourceConfiguration.cs` — schema dwh, unique SourceName
  - `Persistence/Configurations/FactQuoteConfiguration.cs` — schema dwh, UNIQUE(Asset,Date,Source), FK с Restrict
- [✅] **3.5** Создать фабрику для Dapper-подключения:
  - `Persistence/DapperConnectionFactory.cs` — возвращает IDbConnection (SqlConnection или NpgsqlConnection) в зависимости от DatabaseOptions.Provider
- [✅] **3.6** Собрать проект, убедиться что нет ошибок

---

## Фаза 4. Infrastructure Layer — Часть 2: Repositories & Providers

- [✅] **4.1** Реализовать репозитории (EF Core, запись):
  - `Repositories/RawIngestDataRepository.cs` — AddAsync, GetUnprocessedAsync, MarkAsProcessedAsync (ExecuteUpdateAsync)
  - `Repositories/DimAssetRepository.cs` — GetByTickerAsync, UpsertAsync, GetAllActiveAsync
  - `Repositories/DimDateRepository.cs` — EnsureDateExistsAsync (auto-populates all fields), GetByDateAsync
  - `Repositories/DimSourceRepository.cs` — GetByNameAsync, UpsertAsync
  - `Repositories/FactQuoteRepository.cs` — AddRangeAsync, ExistsAsync
- [✅] **4.2** Реализовать DapperDataMartReader (Dapper, чтение):
  - `DataMarts/DapperDataMartReader.cs` — параметризованные SQL-запросы к mart-вьюшкам с CommandDefinition + CancellationToken
- [✅] **4.3** Создать API-клиенты:
  - `Providers/YahooFinanceProvider.cs` — реализация IMarketDataProvider, конвертация Yahoo JSON в стандартный формат
  - `Providers/CoinGeckoProvider.cs` — реализация IMarketDataProvider, конвертация CoinGecko market_chart в стандартный формат
  - Обработка HTTP 429 с экспоненциальным backoff в обоих (MaxRetries=3)
- [✅] **4.4** Собрать проект, убедиться что нет ошибок

---

## Фаза 5. Infrastructure Layer — Часть 3: DI-регистрация и мульти-СУБД

- [✅] **5.1** Создать extension-метод `DependencyInjection/InfrastructureServiceExtensions.cs`:
  - `AddInfrastructure(IServiceCollection, IConfiguration)`:
    - Читает `DatabaseOptions` из конфигурации (Options Pattern) — секция `"Database"`
    - Читает `FeaturesOptions` (UseRedis, UseClickHouse) из конфигурации — секция `"Features"`
    - Регистрирует `FinDistillDbContext` с выбором провайдера:
      - `"SqlServer"` → `UseSqlServer(connectionString)`
      - `"PostgreSQL"` → `UseNpgsql(connectionString)`
      - Иначе → бросить `InvalidOperationException` с понятным сообщением
    - Регистрирует `DapperConnectionFactory` как Singleton
    - Регистрирует все репозитории (Scoped)
    - Регистрирует `IDataMartReader` → `DapperDataMartReader` (ClickHouse — Фаза 11)
    - Регистрирует `ICacheService` → `NullCacheService` (Redis — Фаза 10)
    - Регистрирует API-провайдеры как `IMarketDataProvider` через `AddHttpClient<T>()`
  - Добавлен `Configuration/FeaturesOptions.cs`
  - Добавлен пакет `Microsoft.Extensions.Http` для `AddHttpClient`
- [✅] **5.2** Создать `NullCacheService.cs` — no-op реализация ICacheService (заглушка, всегда возвращает null):
  - Размещение: `Infrastructure/Caching/NullCacheService.cs`
  - Позволяет DashboardService работать прозрачно без Redis
- [✅] **5.3** Создать extension-метод `DependencyInjection/ApplicationServiceExtensions.cs`:
  - Регистрирует ETL-сервисы и DashboardService (Scoped)
  - Добавлен пакет `Microsoft.Extensions.DependencyInjection.Abstractions` в Application
- [✅] **5.4** Собрать проект, убедиться что нет ошибок

---

## Фаза 6. Presentation Layer (FinDistill.Web)

- [✅] **6.1** Добавить NuGet-пакеты: Serilog.AspNetCore 8.0.3 (включает Console + File sinks)
- [✅] **6.2** Настроить `Program.cs`:
  - Serilog bootstrap + Host.UseSerilog (Console + File sinks, rolling daily)
  - `builder.Services.AddInfrastructure(configuration)`
  - `builder.Services.AddApplicationServices()`
  - MVC (`AddControllersWithViews`)
  - Default route → Dashboard/Index
- [✅] **6.3** Настроить `appsettings.json`:
  - ConnectionStrings:DefaultConnection
  - Database:Provider (Options Pattern)
  - Features, DataSources, Serilog секции
- [✅] **6.4** Создать ViewModels:
  - `ViewModels/DashboardViewModel.cs` — DailyPerformance + PortfolioSummary
  - `ViewModels/AssetDetailViewModel.cs` — Ticker, Name, AssetType, LastClose, ChangePercent, History
- [✅] **6.5** Создать Controllers:
  - `Controllers/DashboardController.cs` — Index (дашборд со списком активов)
  - `Controllers/AssetController.cs` — Detail (история и график актива)
  - `Controllers/SyncController.cs` — RunSync (POST, ручной запуск ETL)
- [✅] **6.6** Создать Views (Razor):
  - `Views/Dashboard/Index.cshtml` — таблица с портфелем, кнопка Chart
  - `Views/Asset/Detail.cshtml` — Chart.js график + OHLCV таблица
  - `Views/Shared/_Layout.cshtml` — dark nav, Sync Now кнопка
- [✅] **6.7** Chart.js подключен через CDN (chart.js@4) в Asset/Detail.cshtml
- [✅] **6.8** Собрать проект, убедиться что нет ошибок

---

## Фаза 7. Worker Service (FinDistill.Worker)

- [✅] **7.1** Добавить NuGet-пакеты:
  - Serilog.Extensions.Hosting 8.0.0
  - Serilog.Sinks.Console 5.0.0
  - Serilog.Sinks.File 5.0.0
  - Serilog.Settings.Configuration 8.0.4
- [✅] **7.2** Настроить `Program.cs`:
  - Serilog bootstrap + AddSerilog (Console + File, rolling daily)
  - `builder.Services.AddInfrastructure(configuration)`
  - `builder.Services.AddApplicationServices()`
  - `builder.Services.AddHostedService<EtlWorker>()`
- [✅] **7.3** Настроить `appsettings.json`:
  - ConnectionStrings:DefaultConnection, Database:Provider
  - EtlSchedule (IntervalMinutes=15, CronExpression=null)
  - DataSources, Serilog
- [✅] **7.4** Создать `EtlWorker.cs : BackgroundService`:
  - IServiceScopeFactory для scoped IEtlOrchestrator
  - IOptions<EtlScheduleOptions> для расписания
  - Graceful shutdown через CancellationToken
  - Structured logging через Serilog
- [✅] **7.5** Создать `Configuration/EtlScheduleOptions.cs`
- [✅] **7.6** Собрать проект, убедиться что нет ошибок

---

## Фаза 8. Миграции EF Core

- [✅] **8.1** Создать начальную миграцию для SQL Server:
  - Установлен dotnet-ef 8.0.16 как local tool
  - Добавлен Microsoft.EntityFrameworkCore.Design 8.0.16 в Web
  - Миграция `InitialCreate` — создаёт таблицы в схемах `lake` и `dwh`
- [✅] **8.2** Проверить, что миграция генерирует корректные схемы (lake, dwh):
  - `lake.RawIngestData` — IDENTITY, index on IsProcessed
  - `dwh.DimAssets` — unique Ticker
  - `dwh.DimDates` — ValueGeneratedNever, unique FullDate
  - `dwh.DimSources` — unique SourceName
  - `dwh.FactQuotes` — UNIQUE(Asset,Date,Source), FK Restrict
- [✅] **8.3** Создать SQL-скрипты для Data Mart Views:
  - `Scripts/DataMartViews_SqlServer.sql` — CROSS APPLY / OUTER APPLY
  - `Scripts/DataMartViews_PostgreSQL.sql` — LATERAL JOIN
- [✅] **8.4** Добавить миграцию для создания Views:
  - Миграция `AddDataMartViews` — EnsureSchema mart + raw SQL для 3 views
  - Down: DROP VIEW IF EXISTS

---

## Фаза 9. Финальная сборка и проверка

- [✅] **9.1** `dotnet build` всего solution — 0 errors
- [✅] **9.2** Проверить, что переключение `Database:Provider` между `"SqlServer"` и `"PostgreSQL"` в appsettings корректно меняет провайдер EF Core и Dapper-подключение:
  - `InfrastructureServiceExtensions.AddInfrastructure` — switch по `dbOptions.Provider`
  - `DapperConnectionFactory.CreateConnection` — switch по `_provider`
  - `DapperDataMartReader` — `_isPostgreSql` для SQL-диалекта
- [✅] **9.3** Проверить, что `ICacheService` инжектится как `NullCacheService` при `Features:UseRedis = false`
- [✅] **9.4** Проверить, что `IDataMartReader` инжектится как `DapperDataMartReader` при `Features:UseClickHouse = false`
- [✅] **9.5** Обновить `.github/copilot-instructions.md`:
  - Секция конфигурации: `"DatabaseProvider": "SqlServer"` → `"Database": { "Provider": "SqlServer" }`
- [✅] **9.6** Обновить данный WORKPLAN — пометить все шаги как выполненные

---

## Фаза 9.5. Аудит и критические исправления

> Исправления выявленные при комплексном аудите кода (post Phase 9).

- [✅] **9.5.1** 🔴 Исправить `ExtractorService`: передавать тикеры из `DataSources` конфигурации
  - Создан `Configuration/DataSourcesOptions.cs` в Infrastructure
  - Создан `Interfaces/ITickerProvider.cs` в Application (абстракция)
  - Создан `Providers/ConfigTickerProvider.cs` в Infrastructure (реализация)
  - `ExtractorService` использует `ITickerProvider` для получения тикеров по `SourceType`
  - Добавлен `AddRangeAsync` в `IRawIngestDataRepository` для batch-сохранения
- [✅] **9.5.2** 🟡 Исправить `LoaderService`: `AssetType = "YahooFinance"` → реальный тип
  - Метод `ResolveAssetType`: CoinGecko → "Crypto", YahooFinance → "Stock"
- [✅] **9.5.3** 🟡 Оптимизировать `LoaderService`: устранить N+1 запросы
  - In-memory кэш для dimensions (asset/date/source) внутри batch
  - Один `AddRangeAsync` для всех фактов в конце
- [✅] **9.5.4** 🟡 Вынести retry-логику в `RetryDelegatingHandler`
  - Создан `Infrastructure/Http/RetryDelegatingHandler.cs`
  - Зарегистрирован через `AddHttpMessageHandler` для обоих провайдеров
  - Удалён дублированный retry-код из `YahooFinanceProvider` и `CoinGeckoProvider`
- [✅] **9.5.5** Собрать проект, убедиться что нет ошибок

---

## Фаза 10. Redis-кэширование (опциональная)

> Реализуется только после базового функционала (Фазы 0–9). Интерфейс `ICacheService` уже заложен в Фазе 1.

- [ ] **10.1** Добавить NuGet-пакет `StackExchange.Redis` в FinDistill.Infrastructure
- [ ] **10.2** Создать класс настроек `Configuration/CacheOptions.cs`:
  - DefaultTtlMinutes, KeyPrefix
- [ ] **10.3** Реализовать `Caching/RedisCacheService.cs : ICacheService`:
  - Сериализация через System.Text.Json
  - Подключение через IConnectionMultiplexer (Singleton)
- [ ] **10.4** Обновить `AddInfrastructure`: при `Features:UseRedis = true` → регистрировать `RedisCacheService` вместо `NullCacheService`
- [ ] **10.5** Обновить `DashboardService` — добавить cache-aside логику:
  - `GetAsync` → cache hit → return; cache miss → `IDataMartReader` → `SetAsync` → return
- [ ] **10.6** Проверить работу с Redis включённым и выключенным

---

## Фаза 14. Runtime-исправления и code review (ветка loading-errors-fix)

> Исправления выявленные при первом запуске приложения и code review PR.

- [✅] **14.1** Создать БД `FinDistillBI` и применить EF Core миграции (`dotnet ef database update`)
- [✅] **14.2** 🔴 Исправить DI: `AddHttpClient<IMarketDataProvider, T>` перезаписывал регистрацию
  - Возвращён паттерн `AddHttpClient<T>()` + `AddScoped<IMarketDataProvider, T>()`
  - Оба провайдера теперь корректно разрешаются через `IEnumerable<IMarketDataProvider>`
- [✅] **14.3** Добавить `User-Agent` и `Accept` заголовки в HttpClient для Yahoo и CoinGecko (устраняет 403)
- [✅] **14.4** 🔴 Исправить `RetryDelegatingHandler`: pre-buffer content в `byte[]` до retry loop
  - Заменён `CloneRequestAsync` (async) на синхронный `CloneRequest(byte[])`
  - Устранён баг с consumed stream при retry POST/PUT запросов
- [✅] **14.5** Заменить недостижимый fallback на `throw new InvalidOperationException` в retry handler
- [✅] **14.6** 🔴 Исправить `LoaderService`: intra-batch deduplication через `HashSet<(int,int,int)>`
  - Устранено `DbUpdateException: duplicate key` при повторных запросах к CoinGecko
- [✅] **14.7** Добавить тест `LoadAsync_IntraBatchDuplicates_InsertsOnlyFirst` для новой логики dedup
- [✅] **14.8** Code review: исправить `DateOnlyTypeHandler.Parse` — обработка DateTime/DateOnly/string
- [✅] **14.9** Code review: `RawIngestDataRepositoryIntegrationTests` — фильтровать по `uniqueSource`
- [✅] **14.10** Code review: `SeedTestDataAsync` — idempotent seed для каждой сущности отдельно
- [✅] **14.11** Code review: `Microsoft.EntityFrameworkCore.Design` → добавить `PrivateAssets="all"`
- [✅] **14.12** Code review: `DockerAvailableFactAttribute` — удалить `TryStartDockerDesktop`, убить процесс при timeout
- [✅] **14.13** Собрать проект: build 0 errors, 73 tests pass
