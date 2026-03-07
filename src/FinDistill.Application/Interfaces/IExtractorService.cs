using FinDistill.Domain.Common;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Extracts raw market data from external API sources and stores it in the Data Lake.
/// </summary>
public interface IExtractorService
{
    Task<Result> ExtractAsync(CancellationToken ct);
}
