-- Data Mart Views for FinDistill.BI (PostgreSQL)
-- Applied via EF Core migration: AddDataMartViews

-- mart.v_DailyPerformance
CREATE OR REPLACE VIEW mart."v_DailyPerformance" AS
SELECT
    a."Ticker",
    a."Name",
    a."AssetType",
    fq."ClosePrice",
    CASE
        WHEN prev."ClosePrice" IS NULL OR prev."ClosePrice" = 0 THEN 0
        ELSE ROUND((fq."ClosePrice" - prev."ClosePrice") / prev."ClosePrice" * 100, 2)
    END AS "ChangePercent"
FROM dwh."DimAssets" a
INNER JOIN LATERAL (
    SELECT f."ClosePrice", f."DateKey"
    FROM dwh."FactQuotes" f
    WHERE f."AssetKey" = a."AssetKey"
    ORDER BY f."DateKey" DESC
    LIMIT 1
) fq ON true
LEFT JOIN LATERAL (
    SELECT f2."ClosePrice"
    FROM dwh."FactQuotes" f2
    WHERE f2."AssetKey" = a."AssetKey" AND f2."DateKey" < fq."DateKey"
    ORDER BY f2."DateKey" DESC
    LIMIT 1
) prev ON true
WHERE a."IsActive" = true;

-- mart.v_AssetHistory
CREATE OR REPLACE VIEW mart."v_AssetHistory" AS
SELECT
    a."Ticker",
    d."FullDate" AS "Date",
    fq."OpenPrice" AS "Open",
    fq."HighPrice" AS "High",
    fq."LowPrice" AS "Low",
    fq."ClosePrice" AS "Close",
    fq."Volume"
FROM dwh."FactQuotes" fq
INNER JOIN dwh."DimAssets" a ON a."AssetKey" = fq."AssetKey"
INNER JOIN dwh."DimDates" d ON d."DateKey" = fq."DateKey"
WHERE a."IsActive" = true;

-- mart.v_PortfolioSummary
CREATE OR REPLACE VIEW mart."v_PortfolioSummary" AS
SELECT
    a."Ticker",
    a."Name",
    a."AssetType",
    fq."ClosePrice" AS "LastClose",
    COALESCE(prev."ClosePrice", 0) AS "PreviousClose",
    CASE
        WHEN prev."ClosePrice" IS NULL OR prev."ClosePrice" = 0 THEN 0
        ELSE ROUND((fq."ClosePrice" - prev."ClosePrice") / prev."ClosePrice" * 100, 2)
    END AS "ChangePercent"
FROM dwh."DimAssets" a
INNER JOIN LATERAL (
    SELECT f."ClosePrice", f."DateKey"
    FROM dwh."FactQuotes" f
    WHERE f."AssetKey" = a."AssetKey"
    ORDER BY f."DateKey" DESC
    LIMIT 1
) fq ON true
LEFT JOIN LATERAL (
    SELECT f2."ClosePrice"
    FROM dwh."FactQuotes" f2
    WHERE f2."AssetKey" = a."AssetKey" AND f2."DateKey" < fq."DateKey"
    ORDER BY f2."DateKey" DESC
    LIMIT 1
) prev ON true
WHERE a."IsActive" = true;
