-- ClickHouse DDL for FinDistill.BI DWH tables
-- Engine: ReplacingMergeTree (idempotent inserts — deduplicates by sorting key)

CREATE DATABASE IF NOT EXISTS dwh;

-- DimAssets
CREATE TABLE IF NOT EXISTS dwh.DimAssets
(
    AssetKey   Int32,
    Ticker     String,
    Name       String,
    AssetType  String,
    Exchange   String DEFAULT '',
    IsActive   UInt8  DEFAULT 1,
    CreatedAt  DateTime,
    UpdatedAt  DateTime
)
ENGINE = ReplacingMergeTree(UpdatedAt)
ORDER BY AssetKey;

-- DimDates
CREATE TABLE IF NOT EXISTS dwh.DimDates
(
    DateKey    Int32,
    FullDate   Date,
    Year       Int32,
    Quarter    UInt8,
    Month      UInt8,
    Day        UInt8,
    DayOfWeek  UInt8,
    WeekOfYear UInt8,
    IsWeekend  UInt8 DEFAULT 0
)
ENGINE = ReplacingMergeTree()
ORDER BY DateKey;

-- DimSources
CREATE TABLE IF NOT EXISTS dwh.DimSources
(
    SourceKey  Int32,
    SourceName String,
    BaseUrl    String DEFAULT '',
    IsActive   UInt8  DEFAULT 1
)
ENGINE = ReplacingMergeTree()
ORDER BY SourceKey;

-- FactQuotes
CREATE TABLE IF NOT EXISTS dwh.FactQuotes
(
    Id         Int64,
    AssetKey   Int32,
    DateKey    Int32,
    SourceKey  Int32,
    OpenPrice  Decimal(18, 8),
    HighPrice  Decimal(18, 8),
    LowPrice   Decimal(18, 8),
    ClosePrice Decimal(18, 8),
    Volume     Decimal(18, 4),
    LoadedAt   DateTime
)
ENGINE = ReplacingMergeTree(LoadedAt)
ORDER BY (AssetKey, DateKey, SourceKey);
