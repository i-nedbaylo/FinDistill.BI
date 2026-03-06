using FinDistill.Domain.Models;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Reads pre-aggregated data from Data Marts.
/// Default implementation uses Dapper; can be swapped to ClickHouse via configuration.
/// </summary>
public interface IDataMartReader
{
    Task<IReadOnlyList<DailyPerformanceRecord>> GetDailyPerformanceAsync(CancellationToken ct);

    Task<IReadOnlyList<AssetHistoryRecord>> GetAssetHistoryAsync(string ticker, int days, CancellationToken ct);

    Task<IReadOnlyList<PortfolioSummaryRecord>> GetPortfolioSummaryAsync(CancellationToken ct);
}
