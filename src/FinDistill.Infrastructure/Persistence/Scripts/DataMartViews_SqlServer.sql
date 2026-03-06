-- Data Mart Views for FinDistill.BI
-- Applied via EF Core migration: AddDataMartViews

-- mart.v_DailyPerformance
-- Shows the latest close price and daily change % for each active asset.
CREATE OR ALTER VIEW mart.v_DailyPerformance AS
SELECT
    a.Ticker,
    a.Name,
    a.AssetType,
    fq.ClosePrice,
    CASE
        WHEN prev.ClosePrice IS NULL OR prev.ClosePrice = 0 THEN 0
        ELSE ROUND((fq.ClosePrice - prev.ClosePrice) / prev.ClosePrice * 100, 2)
    END AS ChangePercent
FROM dwh.DimAssets a
CROSS APPLY (
    SELECT TOP 1 f.ClosePrice, f.DateKey
    FROM dwh.FactQuotes f
    WHERE f.AssetKey = a.AssetKey
    ORDER BY f.DateKey DESC
) fq
OUTER APPLY (
    SELECT TOP 1 f2.ClosePrice
    FROM dwh.FactQuotes f2
    WHERE f2.AssetKey = a.AssetKey AND f2.DateKey < fq.DateKey
    ORDER BY f2.DateKey DESC
) prev
WHERE a.IsActive = 1;
GO

-- mart.v_AssetHistory
-- Historical OHLCV data for chart rendering, joined with date and asset info.
CREATE OR ALTER VIEW mart.v_AssetHistory AS
SELECT
    a.Ticker,
    d.FullDate AS [Date],
    fq.OpenPrice AS [Open],
    fq.HighPrice AS [High],
    fq.LowPrice AS [Low],
    fq.ClosePrice AS [Close],
    fq.Volume
FROM dwh.FactQuotes fq
INNER JOIN dwh.DimAssets a ON a.AssetKey = fq.AssetKey
INNER JOIN dwh.DimDates d ON d.DateKey = fq.DateKey
WHERE a.IsActive = 1;
GO

-- mart.v_PortfolioSummary
-- Summary of all tracked assets: last close, previous close, and change %.
CREATE OR ALTER VIEW mart.v_PortfolioSummary AS
SELECT
    a.Ticker,
    a.Name,
    a.AssetType,
    fq.ClosePrice AS LastClose,
    ISNULL(prev.ClosePrice, 0) AS PreviousClose,
    CASE
        WHEN prev.ClosePrice IS NULL OR prev.ClosePrice = 0 THEN 0
        ELSE ROUND((fq.ClosePrice - prev.ClosePrice) / prev.ClosePrice * 100, 2)
    END AS ChangePercent
FROM dwh.DimAssets a
CROSS APPLY (
    SELECT TOP 1 f.ClosePrice, f.DateKey
    FROM dwh.FactQuotes f
    WHERE f.AssetKey = a.AssetKey
    ORDER BY f.DateKey DESC
) fq
OUTER APPLY (
    SELECT TOP 1 f2.ClosePrice
    FROM dwh.FactQuotes f2
    WHERE f2.AssetKey = a.AssetKey AND f2.DateKey < fq.DateKey
    ORDER BY f2.DateKey DESC
) prev
WHERE a.IsActive = 1;
GO
