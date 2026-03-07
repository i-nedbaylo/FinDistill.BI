namespace FinDistill.Application.Interfaces;

/// <summary>
/// Synchronizes DWH data to ClickHouse after the Load stage.
/// Only invoked when Features:UseClickHouse is enabled.
/// </summary>
public interface IClickHouseSyncService
{
    Task SyncAsync(CancellationToken ct);
}
