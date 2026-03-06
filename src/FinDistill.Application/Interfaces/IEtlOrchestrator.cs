namespace FinDistill.Application.Interfaces;

/// <summary>
/// Orchestrates the full ETL pipeline: Extract → Transform → Load.
/// </summary>
public interface IEtlOrchestrator
{
    Task RunEtlPipelineAsync(CancellationToken ct);
}
