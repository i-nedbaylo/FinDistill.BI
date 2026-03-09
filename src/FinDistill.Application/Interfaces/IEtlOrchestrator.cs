namespace FinDistill.Application.Interfaces;

using FinDistill.Domain.Common;

/// <summary>
/// Orchestrates the full ETL pipeline: Extract → Transform → Load.
/// </summary>
public interface IEtlOrchestrator
{
    /// <summary>Runs the complete ETL pipeline and returns the aggregate result.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or the first stage failure.</returns>
    Task<Result> RunEtlPipelineAsync(CancellationToken ct);
}
