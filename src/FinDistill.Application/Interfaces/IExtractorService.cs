using FinDistill.Domain.Common;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Extracts raw market data from external API sources and stores it in the Data Lake.
/// </summary>
public interface IExtractorService
{
    /// <summary>
    /// Runs the extraction stage: fetches data from all enabled providers and persists it to the Data Lake.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or partial failure.
    /// </returns>
    Task<Result> ExtractAsync(CancellationToken ct);
}
