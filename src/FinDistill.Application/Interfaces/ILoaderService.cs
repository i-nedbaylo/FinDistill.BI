using FinDistill.Application.DTOs;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Loads transformed quote data into DWH dimension and fact tables.
/// </summary>
public interface ILoaderService
{
    Task LoadAsync(IEnumerable<ParsedQuoteDto> quotes, CancellationToken ct);
}
