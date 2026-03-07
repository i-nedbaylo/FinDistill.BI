using FinDistill.Application.DTOs;
using FinDistill.Domain.Common;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Loads transformed quote data into DWH dimension and fact tables.
/// </summary>
public interface ILoaderService
{
    Task<Result> LoadAsync(IEnumerable<ParsedQuoteDto> quotes, CancellationToken ct);
}
