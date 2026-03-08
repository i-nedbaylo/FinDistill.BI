namespace FinDistill.Application.Interfaces;

/// <summary>
/// Synchronizes DWH data to ClickHouse after the Load stage.
/// Only invoked when Features:UseClickHouse is enabled.
/// </summary>
public interface IClickHouseSyncService
{
    /// <summary>Synchronizes all DWH dimension and fact data to ClickHouse.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task SyncAsync(CancellationToken ct);
}
