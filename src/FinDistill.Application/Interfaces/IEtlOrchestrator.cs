namespace FinDistill.Application.Interfaces;

using FinDistill.Domain.Common;

/// <summary>
/// Orchestrates the full ETL pipeline: Extract → Transform → Load.
/// </summary>
public interface IEtlOrchestrator
{
    Task<Result> RunEtlPipelineAsync(CancellationToken ct);
}
