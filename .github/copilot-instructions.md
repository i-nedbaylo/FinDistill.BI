# FinDistill.BI — Copilot Instructions

> Этот файл является единственным источником правды для всех Copilot-сессий в данном репозитории.
> Все ответы, генерация кода и архитектурные решения **ДОЛЖНЫ** соответствовать описанным ниже правилам.

---

## 1. Обзор проекта

**FinDistill.BI** — автоматизированная BI-система сбора, хранения и визуализации рыночных котировок (акции, ETF, криптовалюты) с трёхуровневой архитектурой данных (Data Lake → DWH → Data Marts).

---

## 2. Технологический стек

| Категория | Технология | Примечания |
|---|---|---|
| Платформа | .NET 8 или .NET 9 | Целевой TFM: `net8.0` или `net9.0` |
| Язык | C# 12+ | Nullable reference types включены (`<Nullable>enable</Nullable>`) |
| Web-фреймворк | ASP.NET Core MVC | Razor Views, контроллеры, ViewModels |
| ORM (запись) | Entity Framework Core | Миграции, управление схемой, запись в Data Lake / DWH |
| Micro-ORM (чтение) | Dapper | Высокопроизводительное чтение данных из Data Marts |
| СУБД (OLTP) | MS SQL Server **или** PostgreSQL | Код абстрагирован от конкретной СУБД через провайдер EF Core |
| СУБД (OLAP, опц.) | ClickHouse | Опциональный провайдер для Data Marts (аналитические запросы). См. раздел 10 |
| Кэш (опц.) | Redis | Опциональное кэширование Data Marts. См. раздел 10 |
| Контейнеризация | Docker, Docker Compose | Dockerfile для Web и Worker, docker-compose для полного стека |
| Логирование | Serilog | Structured logging; sinks: Console, File (минимум) |
| API-источники | Yahoo Finance (`YahooFinanceApi`), CoinGecko API | Реализация через паттерн Strategy |
| Графики (фронтенд) | Chart.js **или** Highcharts | JS-библиотека для исторических графиков |

### Запрещено

- Не использовать `System.Data.SqlClient` напрямую — только через Dapper / EF Core.
- Не добавлять сторонние ORM помимо EF Core и Dapper.
- Не использовать Blazor, Razor Pages (только MVC pattern).

---

## 3. Структура решения (Clean Architecture)

```
FinDistill.BI.sln
│
├── src/
│   ├── FinDistill.Domain/              — Domain Layer
│   ├── FinDistill.Application/         — Application Layer
│   ├── FinDistill.Infrastructure/      — Infrastructure Layer
│   ├── FinDistill.Web/                 — Presentation Layer (ASP.NET Core MVC)
│   └── FinDistill.Worker/              — Worker Service (ETL Engine)
│
└── tests/
    ├── FinDistill.Domain.Tests/
    ├── FinDistill.Application.Tests/
    └── FinDistill.Infrastructure.Tests/
```

### 3.1 FinDistill.Domain (Class Library)

**Зависимости:** Нет (ни на один другой проект, ни на инфраструктурные пакеты).

Содержит:
- **Сущности (Entities):** `Asset`, `Quote`, `RawDataRecord`, `DimDate`, `DimSource` и т.д.
- **Интерфейсы репозиториев:** `IAssetRepository`, `IQuoteRepository`, `IRawDataRepository` и пр.
- **Интерфейсы сервисов:** `IMarketDataProvider` (Strategy pattern для API-источников).
- **Enums:** `AssetType` (Stock, ETF, Crypto), `DataSourceType` (YahooFinance, CoinGecko).
- **Value Objects и DTO**, если необходимо.

**Правила:**
- Никаких ссылок на EF Core, Dapper, HttpClient, Serilog.
- Только чистые C#-классы и интерфейсы.

### 3.2 FinDistill.Application (Class Library)

**Зависимости:** → `FinDistill.Domain`

Содержит:
- **ETL-сервисы:** `ExtractorService`, `TransformerService`, `LoaderService`.
- **Интерфейсы прикладных сервисов:** `IEtlOrchestrator`, `IDashboardService`.
- **DTO / Models** для передачи между слоями.
- **Валидация и бизнес-правила** (очистка от дублей, приведение к формату DWH).

**Правила:**
- Бизнес-логика ETL живёт здесь, а не в Infrastructure.
- Не зависит от конкретных реализаций (EF Core, HttpClient) — только интерфейсы из Domain.

### 3.3 FinDistill.Infrastructure (Class Library)

**Зависимости:** → `FinDistill.Domain`, → `FinDistill.Application`

Содержит:
- **DbContext** (EF Core): `FinDistillDbContext` — единый контекст для Data Lake и DWH таблиц.
- **Миграции** EF Core.
- **Репозитории** (реализации интерфейсов из Domain): `AssetRepository`, `QuoteRepository` и пр.
- **Dapper-запросы** для Data Mart: `DapperDataMartReader` (или аналог).
- **API-клиенты** (реализации `IMarketDataProvider`):
  - `YahooFinanceProvider` — обёртка над `YahooFinanceApi`.
  - `CoinGeckoProvider` — HTTP-клиент для CoinGecko API.
- **Обработка ошибок API:** Retry-логика с экспоненциальной задержкой для HTTP 429 (Too Many Requests).
- **Кэширование (опц.):** `RedisCacheService` — реализация `ICacheService` из Domain (см. раздел 10).
- **ClickHouse (опц.):** `ClickHouseDataMartReader` — реализация `IDataMartReader` для аналитических запросов (см. раздел 10).
- **DI-регистрация:** Extension-метод `IServiceCollection.AddInfrastructure(IConfiguration)`.

### 3.4 FinDistill.Web (ASP.NET Core MVC)

**Зависимости:** → `FinDistill.Application`, → `FinDistill.Infrastructure` (для DI)

Содержит:
- **Controllers:** `DashboardController`, `AssetController`, `SyncController`.
- **ViewModels:** отдельные классы для каждого View (не использовать Domain-сущности напрямую во View).
- **Views (Razor):** дашборд, детальная страница актива, графики.
- **wwwroot/:** JS (Chart.js / Highcharts), CSS.
- **Ручной запуск синхронизации** через `SyncController` (HTTP POST → вызов ETL-оркестратора).

**Правила:**
- Контроллеры тонкие: логика — в Application-сервисах.
- Не обращаться к репозиториям или DbContext из контроллеров напрямую.

### 3.5 FinDistill.Worker (Worker Service)

**Зависимости:** → `FinDistill.Application`, → `FinDistill.Infrastructure` (для DI)

**Тип проекта:** `Microsoft.NET.Sdk.Worker` (Worker Service).

Содержит:
- **`EtlWorker` : `BackgroundService`** — основной фоновый сервис, запускающий ETL по расписанию.
- **Конфигурация расписания** через `appsettings.json` (`CronExpression` или `IntervalMinutes`).
- **Хостинг:**
  - По умолчанию: консольное приложение.
  - Опционально (через настройку): Windows Service (`UseWindowsService()`) или Docker-контейнер.
  - Выбор режима хостинга через `appsettings.json` → `"HostingMode": "Console" | "WindowsService" | "Docker"`.
  - В режиме `"Docker"` конфигурация передаётся через переменные окружения (ASP.NET Core convention: `ConnectionStrings__DefaultConnection`, `DatabaseProvider` и т.д.).

**Правила:**
- Это **отдельное приложение**, а не библиотека — имеет собственный `Program.cs` и `appsettings.json`.
- Работает автономно и независимо от FinDistill.Web.
- Не содержит бизнес-логику ETL — она в Application; Worker только оркестрирует вызовы.

---

## 4. Архитектура базы данных (3 уровня)

### 4.1 Data Lake (Staging Layer)

Схема: `lake` (или префикс `Lake_`).

```
Table: lake.RawIngestData
├── Id              BIGINT IDENTITY PK
├── Source          NVARCHAR(50)         -- 'YahooFinance', 'CoinGecko'
├── Endpoint        NVARCHAR(256)        -- URL или идентификатор запроса
├── RawContent      NVARCHAR(MAX)        -- Сырой JSON-ответ
├── LoadedAt        DATETIME2            -- UTC timestamp загрузки
└── IsProcessed     BIT DEFAULT 0        -- Флаг обработки Transformer'ом
```

### 4.2 DWH (Core Layer) — Star Schema

Схема: `dwh` (или префикс `Dwh_`).

**Таблицы измерений (Dimensions):**

```
Table: dwh.DimAssets
├── AssetKey         INT IDENTITY PK     -- Surrogate Key
├── Ticker           NVARCHAR(20)        -- 'AAPL', 'BTC'
├── Name             NVARCHAR(200)
├── AssetType        NVARCHAR(20)        -- 'Stock', 'ETF', 'Crypto'
├── Exchange         NVARCHAR(50)        -- Nullable
├── IsActive         BIT DEFAULT 1
├── CreatedAt        DATETIME2
└── UpdatedAt        DATETIME2

Table: dwh.DimDates
├── DateKey          INT PK              -- YYYYMMDD формат
├── FullDate         DATE
├── Year             INT
├── Quarter          TINYINT
├── Month            TINYINT
├── Day              TINYINT
├── DayOfWeek        TINYINT
├── WeekOfYear       TINYINT
└── IsWeekend        BIT

Table: dwh.DimSources
├── SourceKey        INT IDENTITY PK
├── SourceName       NVARCHAR(50)        -- 'YahooFinance', 'CoinGecko'
├── BaseUrl          NVARCHAR(256)
└── IsActive         BIT DEFAULT 1
```

**Таблицы фактов (Facts):**

```
Table: dwh.FactQuotes
├── Id               BIGINT IDENTITY PK
├── AssetKey          INT FK → DimAssets
├── DateKey           INT FK → DimDates
├── SourceKey         INT FK → DimSources
├── OpenPrice         DECIMAL(18,8)
├── HighPrice         DECIMAL(18,8)
├── LowPrice          DECIMAL(18,8)
├── ClosePrice        DECIMAL(18,8)
├── Volume            DECIMAL(18,4)
├── LoadedAt          DATETIME2
└── UNIQUE(AssetKey, DateKey, SourceKey)
```

### 4.3 Data Marts (Access Layer)

Схема: `mart` (или префикс `Mart_`).

Реализуются как SQL Views (или материализованные таблицы):

- **`mart.v_DailyPerformance`** — изменение цены закрытия в % за последние 24 часа для каждого актива.
- **`mart.v_AssetHistory`** — агрегированные исторические данные для построения графиков (дата, OHLCV).
- **`mart.v_PortfolioSummary`** — сводка по всем отслеживаемым активам (последняя цена, изменение).

**Правила чтения Data Marts:**
- Фронтенд (Web) читает **только** из Data Marts через Dapper.
- Запись в Data Lake / DWH — **только** через EF Core.

---

## 5. ETL Pipeline

### 5.1 Extract (E)

- **Кто:** `FinDistill.Worker` → `ExtractorService`.
- **Что:** Опрашивает API-источники по расписанию.
- **Куда:** Сохраняет сырой JSON в `lake.RawIngestData`.
- **Как:** Через реализации `IMarketDataProvider` (Strategy pattern).
- **Обработка ошибок:** HTTP 429 → экспоненциальный backoff; логирование через Serilog.

### 5.2 Transform (T)

- **Кто:** `TransformerService`.
- **Что:** Читает необработанные записи из Lake (`IsProcessed = 0`).
- **Действия:** Парсинг JSON, валидация, очистка дублей, нормализация к формату DWH.
- **Результат:** Подготовленные объекты для загрузки в DWH.

### 5.3 Load (L)

- **Кто:** `LoaderService`.
- **Что:** Записывает трансформированные данные в `dwh.FactQuotes`, обновляет/создаёт записи в Dimension-таблицах.
- **Как:** Через EF Core (batch insert, upsert-логика).
- **После успеха:** Помечает записи в Lake как `IsProcessed = 1`.

---

## 6. Паттерны и соглашения

### 6.1 Strategy Pattern (API-источники)

```csharp
// Domain
public interface IMarketDataProvider
{
    DataSourceType SourceType { get; }
    Task<string> FetchRawDataAsync(string ticker, CancellationToken ct);
    Task<IEnumerable<string>> FetchBulkDataAsync(IEnumerable<string> tickers, CancellationToken ct);
}

// Infrastructure — по одной реализации на источник
public class YahooFinanceProvider : IMarketDataProvider { ... }
public class CoinGeckoProvider : IMarketDataProvider { ... }
```

Регистрация в DI: все реализации регистрируются как `IEnumerable<IMarketDataProvider>`.

### 6.2 Repository Pattern

- Интерфейсы — в Domain.
- Реализации — в Infrastructure.
- Для записи: EF Core.
- Для чтения Data Marts: Dapper (отдельный интерфейс, например `IDataMartReader`).

### 6.3 Именование

| Элемент | Конвенция | Пример |
|---|---|---|
| Namespace | `FinDistill.<Layer>.<Feature>` | `FinDistill.Infrastructure.Providers` |
| Интерфейс | `I` + PascalCase | `IMarketDataProvider` |
| Async-методы | суффикс `Async` | `FetchRawDataAsync` |
| DB-таблицы (EF) | схема + PascalCase | `lake.RawIngestData` |
| Views (SQL) | `v_` + PascalCase | `mart.v_DailyPerformance` |
| ViewModels | суффикс `ViewModel` | `AssetDetailViewModel` |
| Конфигурация | Секции в PascalCase | `"EtlSchedule"`, `"DataSources"` |

### 6.4 Обработка ошибок

- **API rate limits (429):** Retry с экспоненциальным backoff (Polly или ручная реализация).
- **Невалидные данные:** Логировать и пропускать запись, не останавливать весь pipeline.
- **Исключения в ETL:** Ловить на уровне оркестратора, логировать через Serilog, продолжать со следующим тикером.

### 6.5 Логирование (Serilog)

- Каждый этап ETL логируется: начало, количество обработанных записей, ошибки, завершение.
- Structured logging: использовать шаблоны `{PropertyName}`, а не string interpolation.
- Минимальные sinks: Console + File (rolling, по дням).

```csharp
Log.Information("ETL Extract started for {Source}, tickers count: {Count}", source, tickers.Count);
```

---

## 7. Конфигурация (appsettings.json)

Ожидаемые секции конфигурации:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "...",          // Основная СУБД (SQL Server / PostgreSQL)
    "ClickHouse": null,                  // Опционально: строка подключения к ClickHouse
    "Redis": null                        // Опционально: строка подключения к Redis
  },
  "DatabaseProvider": "SqlServer",       // "SqlServer" | "PostgreSQL"
  "EtlSchedule": {
    "IntervalMinutes": 15,               // Интервал запуска ETL в Worker
    "CronExpression": null               // Альтернатива: CRON-выражение
  },
  "DataSources": {
    "YahooFinance": {
      "Enabled": true,
      "Tickers": ["AAPL", "MSFT", "SPY", "QQQ"]
    },
    "CoinGecko": {
      "Enabled": true,
      "CoinIds": ["bitcoin", "ethereum"],
      "VsCurrency": "usd"
    }
  },
  "Features": {
    "UseRedis": false,                   // Включить Redis-кэширование
    "UseClickHouse": false               // Включить ClickHouse для Data Marts
  },
  "HostingMode": "Console",             // "Console" | "WindowsService" | "Docker"
  "Serilog": { ... }
}
```

---

## 8. Зависимости между проектами (строго)

```
FinDistill.Domain         ← (нет зависимостей)
FinDistill.Application    ← Domain
FinDistill.Infrastructure ← Domain, Application
FinDistill.Web            ← Application, Infrastructure
FinDistill.Worker         ← Application, Infrastructure
```

**Запрещённые зависимости:**
- Domain → Application, Infrastructure, Web, Worker.
- Application → Infrastructure, Web, Worker.
- Web ↔ Worker (не зависят друг от друга).

---

## 9. Правила генерации кода Copilot

1. **Всегда** следуй Clean Architecture: не смешивай слои.
2. **Всегда** используй `CancellationToken` в async-методах.
3. **Всегда** проверяй nullable reference types.
4. **Никогда** не помещай бизнес-логику в контроллеры или BackgroundService — только вызов сервисов.
5. **Никогда** не используй Domain-сущности в качестве ViewModel — создавай отдельные ViewModel-классы.
6. **Для записи** в БД — EF Core. **Для чтения** Data Marts — Dapper (или ClickHouse, если включён).
7. **Все API-клиенты** реализуют `IMarketDataProvider` (Strategy pattern).
8. **Логирование** — только Serilog, structured logging.
9. **Обработка 429** — обязательна в каждом API-клиенте.
10. **Worker** — автономное приложение, не библиотека.
11. При создании новых файлов располагай их в соответствующем проекте согласно архитектуре (п. 3).
12. При создании миграций EF Core учитывай схемы БД (`lake`, `dwh`, `mart`).
13. Комментарии в коде — на английском языке.
14. При работе с Dapper всегда используй параметризованные запросы (защита от SQL-injection).
15. Конфигурация — через Options Pattern (`IOptions<T>` / `IOptionsMonitor<T>`).
16. **Все внешние зависимости** (БД, кэш, аналитика) — за интерфейсами в Domain; реализации переключаются через конфигурацию.
17. **Docker-ready:** приложение не должно зависеть от локальных путей; конфигурация через переменные окружения.
18. **Feature Flags:** опциональные компоненты (Redis, ClickHouse) включаются через секцию `Features` в appsettings.

---

## 10. Точки расширения (Extensibility Design)

> Данный раздел описывает интерфейсы и архитектурные решения, которые **закладываются при проектировании**,
> но **реализуются в опциональных фазах** после завершения базового функционала.

### 10.1 Redis — опциональное кэширование

**Цель:** Кэширование результатов Data Mart запросов для снижения нагрузки на СУБД при высокой посещаемости дашборда.

**Архитектурное решение:**

```csharp
// Domain — интерфейс (создаётся сразу в Фазе 1)
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct) where T : class;
    Task RemoveAsync(string key, CancellationToken ct);
}
```

**Реализации (Infrastructure, создаются в Фазе 10):**
- `RedisCacheService : ICacheService` — реализация через `StackExchange.Redis`.
- `NullCacheService : ICacheService` — заглушка (no-op), используется когда Redis отключён.

**Регистрация в DI:**
```csharp
// В AddInfrastructure:
if (features.UseRedis)
    services.AddSingleton<ICacheService, RedisCacheService>();
else
    services.AddSingleton<ICacheService, NullCacheService>();
```

**Использование в Application:**
- `DashboardService` внедряет `ICacheService` и оборачивает вызовы к `IDataMartReader`.
- Кэш-ключи: `$"mart:daily:{ticker}"`, `$"mart:history:{ticker}:{days}"`, `$"mart:portfolio" `.
- TTL: конфигурируется через `CacheOptions` (Options Pattern).

**Правила:**
- Domain и Application **не знают** о Redis — только `ICacheService`.
- `NullCacheService` всегда возвращает `null` из `GetAsync` → логика работает без кэша прозрачно.

### 10.2 ClickHouse — опциональный OLAP-движок для Data Marts

**Цель:** Использовать ClickHouse как высокопроизводительный аналитический движок для чтения Data Marts вместо SQL Views + Dapper.

**Архитектурное решение:**

Интерфейс `IDataMartReader` уже определён в Domain (Фаза 1). Это ключевая точка расширения:

```
IDataMartReader (Domain)
  ├── DapperDataMartReader    — реализация по умолчанию (SQL Server / PostgreSQL)
  └── ClickHouseDataMartReader — опциональная реализация (ClickHouse)
```

**Регистрация в DI:**
```csharp
// В AddInfrastructure:
if (features.UseClickHouse)
    services.AddScoped<IDataMartReader, ClickHouseDataMartReader>();
else
    services.AddScoped<IDataMartReader, DapperDataMartReader>();
```

**Синхронизация данных DWH → ClickHouse:**
- Отдельный этап в ETL Pipeline (после Load): `ClickHouseSyncService`.
- Загружает данные из `dwh.FactQuotes` в таблицы ClickHouse (batch insert).
- Запускается только при `Features:UseClickHouse = true`.

**Пакеты (добавляются только в Фазе 11):**
- `ClickHouse.Client` — ADO.NET провайдер.
- Или `Octonica.ClickHouseClient` — альтернативный провайдер.

**Правила:**
- ClickHouse используется **только для чтения** Data Marts.
- Запись OLTP-данных (Lake, DWH) **всегда** через EF Core в SQL Server / PostgreSQL.
- Application-слой не знает о ClickHouse — только интерфейс `IDataMartReader`.

### 10.3 Docker — контейнеризация

**Цель:** Полный деплой стека (Web + Worker + DB + опционально Redis/ClickHouse) через Docker Compose.

**Архитектурные решения (закладываются сразу):**

1. **Конфигурация через переменные окружения:**
   - Все настройки из `appsettings.json` переопределяются через env vars.
   - ASP.NET Core convention: `ConnectionStrings__DefaultConnection`, `DatabaseProvider`, `Features__UseRedis`.

2. **Health Checks (готовность):**
   - Web: health check endpoint (`/health`) для проверки подключения к БД.
   - Worker: логирует статус подключения при старте.

3. **Graceful Shutdown:**
   - Worker: `BackgroundService` корректно останавливается при `SIGTERM` (через `CancellationToken`).
   - Все async-методы поддерживают `CancellationToken` (правило 2 из раздела 9).

4. **Docker Compose профили:**
   ```yaml
   # docker-compose.yml
   services:
     db-sqlserver:          # профиль: sqlserver
     db-postgres:           # профиль: postgres
     redis:                 # профиль: redis (опционально)
     clickhouse:            # профиль: clickhouse (опционально)
     web:                   # всегда
     worker:                # всегда
   ```

**Правила:**
- Код приложения **не зависит** от Docker — Docker лишь упаковывает и оркестрирует.
- Никаких hardcoded путей, портов, хостнеймов — всё через конфигурацию.

### 10.4 Сводная таблица Feature Flags

| Feature | Настройка | По умолчанию | Интерфейс (Domain) | Реализация по умолчанию | Опциональная реализация |
|---|---|---|---|---|---|
| Redis | `Features:UseRedis` | `false` | `ICacheService` | `NullCacheService` | `RedisCacheService` |
| ClickHouse | `Features:UseClickHouse` | `false` | `IDataMartReader` | `DapperDataMartReader` | `ClickHouseDataMartReader` |
| Docker | `HostingMode` | `"Console"` | — | — | Конфигурация через env vars |
