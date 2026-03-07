using System.Text.Json;
using FinDistill.Application.DTOs;
using FinDistill.Application.Interfaces;
using FinDistill.Domain.Common;
using FinDistill.Domain.Enums;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinDistill.Application.Services;

/// <summary>
/// Reads unprocessed records from the Data Lake, parses JSON, validates data,
/// and returns normalized ParsedQuoteDto records ready for DWH loading.
/// </summary>
public class TransformerService : ITransformerService
{
    private readonly IRawIngestDataRepository _rawRepo;
    private readonly ILogger<TransformerService> _logger;

    public TransformerService(
        IRawIngestDataRepository rawRepo,
        ILogger<TransformerService> logger)
    {
        _rawRepo = rawRepo;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<ParsedQuoteDto>>> TransformAsync(CancellationToken ct)
    {
        try
        {
            var unprocessed = await _rawRepo.GetUnprocessedAsync(ct);
            _logger.LogInformation("ETL Transform started, unprocessed records: {Count}", unprocessed.Count);

            var results = new List<ParsedQuoteDto>();
            var processedIds = new List<long>();

            foreach (var record in unprocessed)
            {
                try
                {
                    var parsed = ParseRecord(record);
                    results.AddRange(parsed);
                    processedIds.Add(record.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ETL Transform skipped invalid record {RecordId} from {Source}", record.Id, record.Source);
                }
            }

            if (processedIds.Count > 0)
            {
                await _rawRepo.MarkAsProcessedAsync(processedIds, ct);
            }

            _logger.LogInformation("ETL Transform completed, parsed quotes: {Count}, processed records: {Processed}",
                results.Count, processedIds.Count);

            return Result.Success<IReadOnlyList<ParsedQuoteDto>>(results);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL Transform failed with unhandled exception");
            return Result.Failure<IReadOnlyList<ParsedQuoteDto>>(
                new Error("Transform.Failed", ex.Message));
        }
    }

    private static List<ParsedQuoteDto> ParseRecord(Domain.Entities.RawIngestData record)
    {
        if (!Enum.TryParse<DataSourceType>(record.Source, ignoreCase: true, out var sourceType))
        {
            throw new InvalidOperationException($"Unknown data source: {record.Source}");
        }

        using var doc = JsonDocument.Parse(record.RawContent);
        var root = doc.RootElement;

        // Support both single-object and array JSON structures
        IEnumerable<JsonElement> elements = root.ValueKind == JsonValueKind.Array
            ? root.EnumerateArray().ToList()
            : [root];

        var quotes = new List<ParsedQuoteDto>();

        foreach (var element in elements)
        {
            var ticker = element.GetProperty("ticker").GetString() ?? string.Empty;
            var dateStr = element.GetProperty("date").GetString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(ticker) || !DateOnly.TryParse(dateStr, out var date))
            {
                continue;
            }

            quotes.Add(new ParsedQuoteDto
            {
                Ticker = ticker,
                Date = date,
                Open = element.TryGetProperty("open", out var o) ? o.GetDecimal() : 0,
                High = element.TryGetProperty("high", out var h) ? h.GetDecimal() : 0,
                Low = element.TryGetProperty("low", out var l) ? l.GetDecimal() : 0,
                Close = element.TryGetProperty("close", out var c) ? c.GetDecimal() : 0,
                Volume = element.TryGetProperty("volume", out var v) ? v.GetDecimal() : 0,
                SourceType = sourceType
            });
        }

        return quotes;
    }
}
