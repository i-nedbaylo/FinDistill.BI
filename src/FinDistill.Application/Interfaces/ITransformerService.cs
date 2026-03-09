using FinDistill.Application.DTOs;
using FinDistill.Domain.Common;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Reads unprocessed records from the Data Lake, parses and validates them,
/// and returns normalized DTOs ready for loading into DWH.
/// </summary>
public interface ITransformerService
{
    /// <summary>Transforms all unprocessed Data Lake records into validated quote DTOs.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the list of parsed quotes, or a failure.</returns>
    Task<Result<IReadOnlyList<ParsedQuoteDto>>> TransformAsync(CancellationToken ct);
}
