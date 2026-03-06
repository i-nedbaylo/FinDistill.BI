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

- [ ] **1.1** Создать Enums:
  - `Enums/AssetType.cs` — Stock, ETF, Crypto
  - `Enums/DataSourceType.cs` — YahooFinance, CoinGecko
- [ ] **1.2** Создать Entities (Data Lake):
  - `Entities/RawIngestData.cs` — Id, Source, Endpoint, RawContent, LoadedAt, IsProcessed
- [ ] **1.3** Создать Entities (DWH — Dimensions):
  - `Entities/DimAsset.cs` — AssetKey, Ticker, Name, AssetType, Exchange, IsActive, CreatedAt, UpdatedAt
  - `Entities/DimDate.cs` — DateKey, FullDate, Year, Quarter, Month, Day, DayOfWeek, WeekOfYear, IsWeekend
  - `Entities/DimSource.cs` — SourceKey, SourceName, BaseUrl, IsActive
- [ ] **1.4** Создать Entities (DWH — Facts):
  - `Entities/FactQuote.cs` — Id, AssetKey, DateKey, SourceKey, OpenPrice, HighPrice, LowPrice, ClosePrice, Volume, LoadedAt + навигационные свойства
- [ ] **1.5** Создать интерфейсы репозиториев:
  - `Interfaces/IRawIngestDataRepository.cs` — AddAsync, GetUnprocessedAsync, MarkAsProcessedAsync
  - `Interfaces/IDimAssetRepository.cs` — GetByTickerAsync, UpsertAsync, GetAllActiveAsync
  - `Interfaces/IDimDateRepository.cs` — EnsureDateExistsAsync, GetByDateAsync
  - `Interfaces/IDimSourceRepository.cs` — GetByNameAsync, UpsertAsync
  - `Interfaces/IFactQuoteRepository.cs` — AddRangeAsync, ExistsAsync (по AssetKey+DateKey+SourceKey)
- [ ] **1.6** Создать интерфейс IMarketDataProvider (Strategy):
  - `Interfaces/IMarketDataProvider.cs` — SourceType, FetchRawDataAsync, FetchBulkDataAsync
- [ ] **1.7** Создать интерфейс IDataMartReader (Dapper/ClickHouse-чтение):
  - `Interfaces/IDataMartReader.cs` — GetDailyPerformanceAsync, GetAssetHistoryAsync, GetPortfolioSummaryAsync
  - ⚡ Ключевая точка расширения: реализация переключается между Dapper и ClickHouse через конфигурацию
- [ ] **1.8** Создать интерфейс ICacheService (точка расширения для Redis):
  - `Interfaces/ICacheService.cs` — GetAsync<T>, SetAsync<T>, RemoveAsync
  - ⚡ Создаётся сразу, реализация — в Фазе 10 (NullCacheService + RedisCacheService)
- [ ] **1.9** Собрать проект, убедиться что нет ошибок

---

## Фаза 2. Application Layer (FinDistill.Application)

- [ ] **2.1** Создать DTO:
  - `DTOs/ParsedQuoteDto.cs` — Ticker, Date, Open, High, Low, Close, Volume, SourceType
  - `DTOs/DailyPerformanceDto.cs` — Ticker, Name, AssetType, ClosePrice, ChangePercent
  - `DTOs/AssetHistoryDto.cs` — Date, Open, High, Low, Close, Volume
  - `DTOs/PortfolioSummaryDto.cs` — Ticker, Name, AssetType, LastClose, PreviousClose, ChangePercent
- [ ] **2.2** Создать интерфейсы ETL-сервисов:
  - `Interfaces/IExtractorService.cs` — ExtractAsync(CancellationToken)
  - `Interfaces/ITransformerService.cs` — TransformAsync(CancellationToken)
  - `Interfaces/ILoaderService.cs` — LoadAsync(IEnumerable<ParsedQuoteDto>, CancellationToken)
  - `Interfaces/IEtlOrchestrator.cs` — RunEtlPipelineAsync(CancellationToken)
- [ ] **2.3** Создать интерфейс IDashboardService:
  - `Interfaces/IDashboardService.cs` — GetDailyPerformanceAsync, GetAssetHistoryAsync, GetPortfolioSummaryAsync
- [ ] **2.4** Реализовать ETL-сервисы:
  - `Services/ExtractorService.cs` — использует IEnumerable<IMarketDataProvider> + IRawIngestDataRepository
  - `Services/TransformerService.cs` — читает Lake, парсит JSON, валидирует, возвращает ParsedQuoteDto
  - `Services/LoaderService.cs` — записывает в DWH через репозитории, помечает Lake как обработанный
  - `Services/EtlOrchestrator.cs` — оркестрирует E→T→L, ловит исключения, логирует
- [ ] **2.5** Реализовать DashboardService:
  - `Services/DashboardService.cs` — делегирует чтение в IDataMartReader
- [ ] **2.6** Собрать проект, убедиться что нет ошибок

---

## Фаза 3. Infrastructure Layer — Часть 1: Database (FinDistill.Infrastructure)

- [ ] **3.1** Добавить NuGet-пакеты:
  - `Microsoft.EntityFrameworkCore`
  - `Microsoft.EntityFrameworkCore.SqlServer`
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `Dapper`
  - `Microsoft.Data.SqlClient` (для Dapper + SQL Server)
  - `Npgsql` (для Dapper + PostgreSQL)
- [ ] **3.2** Создать класс настроек `Configuration/DatabaseOptions.cs`:
  - `Provider` — "SqlServer" | "PostgreSQL"
  - (connection string берётся из ConnectionStrings:DefaultConnection)
- [ ] **3.3** Создать `Persistence/FinDistillDbContext.cs`:
  - DbSet для: RawIngestData, DimAsset, DimDate, DimSource, FactQuote
  - OnModelCreating: конфигурация схем (lake, dwh), ключей, индексов, FK, UNIQUE constraint
- [ ] **3.4** Создать Fluent-конфигурации EF Core (EntityTypeConfiguration):
  - `Persistence/Configurations/RawIngestDataConfiguration.cs`
  - `Persistence/Configurations/DimAssetConfiguration.cs`
  - `Persistence/Configurations/DimDateConfiguration.cs`
  - `Persistence/Configurations/DimSourceConfiguration.cs`
  - `Persistence/Configurations/FactQuoteConfiguration.cs`
- [ ] **3.5** Создать фабрику для Dapper-подключения:
  - `Persistence/DapperConnectionFactory.cs` — возвращает IDbConnection (SqlConnection или NpgsqlConnection) в зависимости от DatabaseOptions.Provider
- [ ] **3.6** Собрать проект, убедиться что нет ошибок

---

## Фаза 4. Infrastructure Layer — Часть 2: Repositories & Providers

- [ ] **4.1** Реализовать репозитории (EF Core, запись):
  - `Repositories/RawIngestDataRepository.cs`
  - `Repositories/DimAssetRepository.cs`
  - `Repositories/DimDateRepository.cs`
  - `Repositories/DimSourceRepository.cs`
  - `Repositories/FactQuoteRepository.cs`
- [ ] **4.2** Реализовать DapperDataMartReader (Dapper, чтение):
  - `DataMarts/DapperDataMartReader.cs` — параметризованные SQL-запросы к mart-вьюшкам
- [ ] **4.3** Создать API-клиенты:
  - `Providers/YahooFinanceProvider.cs` — реализация IMarketDataProvider
  - `Providers/CoinGeckoProvider.cs` — реализация IMarketDataProvider
  - Обработка HTTP 429 с экспоненциальным backoff в обоих
- [ ] **4.4** Собрать проект, убедиться что нет ошибок

---

## Фаза 5. Infrastructure Layer — Часть 3: DI-регистрация и мульти-СУБД

- [ ] **5.1** Создать extension-метод `DependencyInjection/InfrastructureServiceExtensions.cs`:
  - `AddInfrastructure(IServiceCollection, IConfiguration)`:
    - Читает `DatabaseOptions` из конфигурации (Options Pattern)
    - Читает `FeaturesOptions` (UseRedis, UseClickHouse) из конфигурации
    - Регистрирует `FinDistillDbContext` с выбором провайдера:
      - `"SqlServer"` → `UseSqlServer(connectionString)`
      - `"PostgreSQL"` → `UseNpgsql(connectionString)`
      - Иначе → бросить `InvalidOperationException` с понятным сообщением
    - Регистрирует `DapperConnectionFactory` как Singleton
    - Регистрирует все репозитории (Scoped)
    - Регистрирует `IDataMartReader`:
      - По умолчанию → `DapperDataMartReader`
      - Если `Features:UseClickHouse = true` → `ClickHouseDataMartReader` (только после Фазы 11)
    - Регистрирует `ICacheService`:
      - По умолчанию → `NullCacheService`
      - Если `Features:UseRedis = true` → `RedisCacheService` (только после Фазы 10)
    - Регистрирует API-провайдеры как `IMarketDataProvider` (все реализации)
- [ ] **5.2** Создать `NullCacheService.cs` — no-op реализация ICacheService (заглушка, всегда возвращает null):
  - Размещение: `Infrastructure/Caching/NullCacheService.cs`
  - Позволяет DashboardService работать прозрачно без Redis
- [ ] **5.3** Создать extension-метод `DependencyInjection/ApplicationServiceExtensions.cs`:
  - Регистрирует ETL-сервисы и DashboardService
- [ ] **5.4** Собрать проект, убедиться что нет ошибок

---

## Фаза 6. Presentation Layer (FinDistill.Web)

- [ ] **6.1** Добавить NuGet-пакеты: Serilog.AspNetCore, Serilog.Sinks.File, Serilog.Sinks.Console
- [ ] **6.2** Настроить `Program.cs`:
  - Serilog (Console + File sinks)
  - `builder.Services.AddInfrastructure(configuration)`
  - `builder.Services.AddApplicationServices()`
  - MVC (`AddControllersWithViews`)
- [ ] **6.3** Настроить `appsettings.json`:
  - ConnectionStrings:DefaultConnection
  - **DatabaseProvider: "SqlServer" | "PostgreSQL"** ← ключевая настройка
  - EtlSchedule, DataSources, Serilog секции
- [ ] **6.4** Создать ViewModels:
  - `ViewModels/DashboardViewModel.cs`
  - `ViewModels/AssetDetailViewModel.cs`
- [ ] **6.5** Создать Controllers:
  - `Controllers/DashboardController.cs` — Index (дашборд со списком активов)
  - `Controllers/AssetController.cs` — Detail (история и график актива)
  - `Controllers/SyncController.cs` — RunSync (POST, ручной запуск ETL)
- [ ] **6.6** Создать Views (Razor):
  - `Views/Dashboard/Index.cshtml`
  - `Views/Asset/Detail.cshtml`
  - `Views/Shared/_Layout.cshtml`
- [ ] **6.7** Добавить Chart.js в `wwwroot/lib/` и JS для графиков
- [ ] **6.8** Собрать проект, убедиться что нет ошибок

---

## Фаза 7. Worker Service (FinDistill.Worker)

- [ ] **7.1** Добавить NuGet-пакеты: Serilog.Extensions.Hosting, Serilog.Sinks.File, Serilog.Sinks.Console, Microsoft.Extensions.Hosting.WindowsServices
- [ ] **7.2** Настроить `Program.cs`:
  - Serilog
  - `builder.Services.AddInfrastructure(configuration)`
  - `builder.Services.AddApplicationServices()`
  - `builder.Services.AddHostedService<EtlWorker>()`
  - Выбор режима хостинга по настройке `HostingMode`:
    - `"Console"` → стандартный запуск (по умолчанию)
    - `"WindowsService"` → `UseWindowsService()`
    - `"Docker"` → стандартный запуск (без специального кода, но конфигурация через переменные окружения)
- [ ] **7.3** Настроить `appsettings.json`:
  - ConnectionStrings:DefaultConnection
  - **DatabaseProvider: "SqlServer" | "PostgreSQL"** ← ключевая настройка
  - EtlSchedule (IntervalMinutes / CronExpression)
  - DataSources, HostingMode ("Console" | "WindowsService" | "Docker"), Serilog
- [ ] **7.4** Создать `EtlWorker.cs : BackgroundService`:
  - Читает расписание из IOptions<EtlScheduleOptions>
  - В цикле вызывает IEtlOrchestrator.RunEtlPipelineAsync
  - Логирует через Serilog
- [ ] **7.5** Собрать проект, убедиться что нет ошибок

---

## Фаза 7.5. Docker-поддержка

- [ ] **7.5.1** Создать `src/FinDistill.Worker/Dockerfile`:
  - Multi-stage build (SDK → Runtime)
  - Base image: `mcr.microsoft.com/dotnet/runtime:8.0` (Worker не нуждается в ASP.NET)
  - Копирование всех проектов для restore (Domain, Application, Infrastructure, Worker)
  - `ENTRYPOINT ["dotnet", "FinDistill.Worker.dll"]`
- [ ] **7.5.2** Создать `src/FinDistill.Web/Dockerfile`:
  - Multi-stage build (SDK → Runtime)
  - Base image: `mcr.microsoft.com/dotnet/aspnet:8.0`
  - `EXPOSE 8080`
  - `ENTRYPOINT ["dotnet", "FinDistill.Web.dll"]`
- [ ] **7.5.3** Создать `docker-compose.yml` в корне решения:
  - Сервис `web` — собирается из `src/FinDistill.Web/Dockerfile`
  - Сервис `worker` — собирается из `src/FinDistill.Worker/Dockerfile`
  - Сервис `db-sqlserver` — профиль `sqlserver` (mcr.microsoft.com/mssql/server:2022-latest)
  - Сервис `db-postgres` — профиль `postgres` (postgres:16)
  - Сервис `redis` — профиль `redis` (redis:7-alpine), опциональный
  - Сервис `clickhouse` — профиль `clickhouse` (clickhouse/clickhouse-server:latest), опциональный
  - Переменные окружения: `ConnectionStrings__DefaultConnection`, `DatabaseProvider`, `Features__UseRedis`, `Features__UseClickHouse`, `HostingMode=Docker`
  - Volumes для персистентного хранения БД
  - depends_on: web → db, worker → db
- [ ] **7.5.4** Создать `docker-compose.override.yml` с development-настройками:
  - Порты для отладки
  - Переменные окружения для development
- [ ] **7.5.5** Создать `.dockerignore` в корне решения:
  - bin/, obj/, .git/, .vs/, *.user, .github/
- [ ] **7.5.6** Проверить `docker-compose build` — образы собираются без ошибок
- [ ] **7.5.7** Проверить `docker-compose up` — все сервисы стартуют, Worker подключается к БД

---

## Фаза 8. Миграции EF Core

- [ ] **8.1** Создать начальную миграцию для SQL Server
- [ ] **8.2** Проверить, что миграция генерирует корректные схемы (lake, dwh)
- [ ] **8.3** Создать SQL-скрипты для Data Mart Views (mart.v_DailyPerformance, mart.v_AssetHistory, mart.v_PortfolioSummary)
- [ ] **8.4** Добавить миграцию для создания Views

---

## Фаза 9. Финальная сборка и проверка

- [ ] **9.1** `dotnet build` всего solution — 0 errors
- [ ] **9.2** Проверить, что переключение `DatabaseProvider` между `"SqlServer"` и `"PostgreSQL"` в appsettings корректно меняет провайдер EF Core и Dapper-подключение
- [ ] **9.3** Проверить, что `ICacheService` инжектится как `NullCacheService` при `Features:UseRedis = false`
- [ ] **9.4** Проверить, что `IDataMartReader` инжектится как `DapperDataMartReader` при `Features:UseClickHouse = false`
- [ ] **9.5** Обновить `.github/copilot-instructions.md` — добавить секцию про настройку DatabaseProvider
- [ ] **9.6** Обновить данный WORKPLAN — пометить все шаги как выполненные

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
- [ ] **10.6** Добавить Redis в `docker-compose.yml` (профиль `redis`)
- [ ] **10.7** Проверить работу с Redis включённым и выключенным

---

## Фаза 11. ClickHouse для Data Marts (опциональная)

> Реализуется только после базового функционала (Фазы 0–9). Интерфейс `IDataMartReader` уже заложен в Фазе 1.

- [ ] **11.1** Добавить NuGet-пакет `ClickHouse.Client` в FinDistill.Infrastructure
- [ ] **11.2** Создать класс настроек `Configuration/ClickHouseOptions.cs`:
  - ConnectionString (из ConnectionStrings:ClickHouse)
- [ ] **11.3** Реализовать `DataMarts/ClickHouseDataMartReader.cs : IDataMartReader`:
  - SQL-запросы адаптированные под ClickHouse SQL-диалект
  - Параметризованные запросы
- [ ] **11.4** Создать `ClickHouseSyncService` — ETL-этап синхронизации DWH → ClickHouse:
  - Batch insert из dwh.FactQuotes + Dimensions в таблицы ClickHouse
  - Вызывается после LoaderService в оркестраторе (только при UseClickHouse = true)
- [ ] **11.5** Создать DDL-скрипты для таблиц ClickHouse (MergeTree engine)
- [ ] **11.6** Обновить `AddInfrastructure`: при `Features:UseClickHouse = true` → регистрировать `ClickHouseDataMartReader` вместо `DapperDataMartReader`
- [ ] **11.7** Обновить `EtlOrchestrator` — вызывать `ClickHouseSyncService` после Load (при включённом флаге)
- [ ] **11.8** Добавить ClickHouse в `docker-compose.yml` (профиль `clickhouse`)
- [ ] **11.9** Проверить работу с ClickHouse включённым и выключенным
