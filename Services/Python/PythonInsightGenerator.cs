using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FinancialIntelligence.Api.Services;

public sealed class PythonInsightGenerator : IInsightAnalyzer
{
    private readonly IPythonInsightRunner _pythonInsightRunner;
    private readonly ILogger<PythonInsightGenerator> _logger;

    public PythonInsightGenerator(
        IPythonInsightRunner pythonInsightRunner,
        ILogger<PythonInsightGenerator> logger)
    {
        _pythonInsightRunner = pythonInsightRunner;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InsightDto>> AnalyzeAsync(
        string scriptKey,
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scriptKey))
            throw new ArgumentException("Script key is required.", nameof(scriptKey));

        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId is required.", nameof(businessId));

        if (monthsBack <= 0)
            throw new ArgumentOutOfRangeException(nameof(monthsBack), "monthsBack must be greater than zero.");

        var asOfDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        PythonInsightProcessResult response;

        try
        {
            response = await _pythonInsightRunner.RunAsync(
                scriptKey,
                businessId.ToString(),
                asOfDate,
                monthsBack,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Python insight failed. ScriptKey={ScriptKey}, BusinessId={BusinessId}",
                scriptKey,
                businessId);

            return Array.Empty<InsightDto>();
        }

        if (response is null)
        {
            _logger.LogWarning(
                "Python insight returned null response. ScriptKey={ScriptKey}, BusinessId={BusinessId}",
                scriptKey,
                businessId);

            return Array.Empty<InsightDto>();
        }

        if (!string.IsNullOrWhiteSpace(response.Error))
        {
            _logger.LogWarning(
                "Python insight returned an error. ScriptKey={ScriptKey}, BusinessId={BusinessId}, Error={Error}",
                scriptKey,
                businessId,
                response.Error);

            return Array.Empty<InsightDto>();
        }

        if (response.Insights is null || response.Insights.Count == 0)
        {
            return Array.Empty<InsightDto>();
        }

        return response.Insights
            .Select(x => MapToInsightDto(businessId, x))
            .ToList();
    }

    private static InsightDto MapToInsightDto(Guid businessId, PythonInsightDto source)
    {
        return new InsightDto
        {
            BusinessId = businessId,
            Type = source.Type,
            Title = source.Title,
            Description = source.Description,
            Severity = source.Severity,
            EstimatedImpact = source.EstimatedImpact,
            Recommendation = source.Recommendation,
            Score = source.Score,
            Metrics = ConvertMetrics(source.Metrics),
            VisualizationType = source.VisualizationType,
            Visualization = MapVisualization(source.Visualization)
        };
    }

    private static Dictionary<string, object> ConvertMetrics(Dictionary<string, object>? metrics)
    {
        if (metrics is null || metrics.Count == 0)
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in metrics)
        {
            result[kvp.Key] = ConvertJsonValue(kvp.Value);
        }

        return result;
    }

    private static object ConvertJsonValue(object value)
    {
        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                JsonValueKind.Null => string.Empty,
                _ => element.ToString()
            };
        }

        return value;
    }

    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            JsonValueKind.Null => string.Empty,
            _ => element.ToString()
        };
    }

    private static InsightVisualizationDto? MapVisualization(PythonVisualizationDto? source)
    {
        if (source is null)
            return null;

        return new InsightVisualizationDto
        {
            Title = source.Title ?? "Insight visualization",
            Labels = source.Labels,
            HighlightIndexes = source.HighlightIndexes,
            Series = source.Series
                .Select(s => new InsightVisualizationSeriesDto
                {
                    Name = s.Name,
                    Values = s.Values
                })
                .ToList()
        };
    }
}