using FinDistill.Application.DTOs;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Reads unprocessed records from the Data Lake, parses and validates them,
/// and returns normalized DTOs ready for loading into DWH.
/// </summary>
public interface ITransformerService
{
    Task<IReadOnlyList<ParsedQuoteDto>> TransformAsync(CancellationToken ct);
}
