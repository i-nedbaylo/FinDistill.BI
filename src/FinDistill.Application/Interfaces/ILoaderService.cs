using FinDistill.Application.DTOs;
using FinDistill.Domain.Common;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Loads transformed quote data into DWH dimension and fact tables.
/// </summary>
public interface ILoaderService
{
    /// <summary>Upserts dimension records and inserts new fact quotes, skipping duplicates.</summary>
    /// <param name="quotes">Parsed and validated quote DTOs produced by the Transformer stage.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> LoadAsync(IEnumerable<ParsedQuoteDto> quotes, CancellationToken ct);
}
