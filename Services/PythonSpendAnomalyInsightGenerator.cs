
using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Repositories;
using FinancialIntelligence.Api.Services;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinancialIntelligence.Api.Services;

public sealed class PythonSpendAnomalyInsightGenerator : IInsightAnalyzer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PythonSpendAnomalyInsightGenerator> _logger;

    public PythonSpendAnomalyInsightGenerator(
        IConfiguration configuration,
        ILogger<PythonSpendAnomalyInsightGenerator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    public async Task<IReadOnlyList<InsightDto>> AnalyzeAsync(
    Guid businessId,
    int monthsBack,
    CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId is required.", nameof(businessId));

        if (monthsBack <= 0)
            throw new ArgumentOutOfRangeException(nameof(monthsBack), "monthsBack must be greater than zero.");

        var pythonExe = _configuration["PythonInsights:PythonExe"]
            ?? throw new InvalidOperationException("PythonInsights:PythonExe is missing.");

        var scriptPath = _configuration["PythonInsights:ScriptPath"]
            ?? throw new InvalidOperationException("PythonInsights:ScriptPath is missing.");

        var workingDirectory = _configuration["PythonInsights:WorkingDirectory"]
            ?? Path.GetDirectoryName(scriptPath)
            ?? throw new InvalidOperationException("Could not determine Python working directory.");

        var connectionString = _configuration.GetConnectionString("OdbcConnectionString")
            ?? throw new InvalidOperationException("Connection string 'OdbcConnectionString' is missing.");

        var asOfDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var arguments =
            $"\"{scriptPath}\" " +
            $"--connectionString \"{connectionString}\" " +
            $"--businessId \"{businessId}\" " +
            $"--asOfDate \"{asOfDate}\" " +
            $"--monthsBack {monthsBack}";

        _logger.LogInformation(
            "Launching Python spend anomaly. PythonExe={PythonExe}, ScriptPath={ScriptPath}, WorkingDirectory={WorkingDirectory}",
            pythonExe,
            scriptPath,
            workingDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            _logger.LogWarning("Python spend anomaly stderr: {Stderr}", stderr);
        }

        if (process.ExitCode != 0)
        {
            _logger.LogError(
                "Python spend anomaly failed. ExitCode={ExitCode}, Stdout={Stdout}, Stderr={Stderr}",
                process.ExitCode,
                stdout,
                stderr);

            return Array.Empty<InsightDto>();
        }

        PythonSpendAnomalyResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<PythonSpendAnomalyResponse>(
                stdout,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize Python spend anomaly response. Stdout={Stdout}", stdout);
            return Array.Empty<InsightDto>();
        }

        if (response is null)
        {
            _logger.LogWarning("Python spend anomaly returned null response.");
            return Array.Empty<InsightDto>();
        }

        if (!string.IsNullOrWhiteSpace(response.Error))
        {
            _logger.LogWarning(
                "Python spend anomaly returned an error for business {BusinessId}: {Error}",
                businessId,
                response.Error);

            return Array.Empty<InsightDto>();
        }

        if (response.Insights.Count == 0)
        {
            return Array.Empty<InsightDto>();
        }

        return response.Insights
            .Select(x => MapToInsightDto(businessId, x))
            .ToList();
    }
    // public async Task<IReadOnlyList<InsightDto>> AnalyzeAsync(
    //     Guid businessId,
    //     int monthsBack,
    //     CancellationToken cancellationToken = default)
    // {
    //     if (businessId == Guid.Empty)
    //         throw new ArgumentException("BusinessId is required.", nameof(businessId));

    //     if (monthsBack <= 0)
    //         throw new ArgumentOutOfRangeException(nameof(monthsBack), "monthsBack must be greater than zero.");

    //     var pythonExe = _configuration["PythonInsights:PythonExe"] ?? "python";
    //     var scriptPath = _configuration["PythonInsights:ScriptPath"]
    //         ?? throw new InvalidOperationException("PythonInsights:ScriptPath is missing.");
    //     var connectionString = _configuration.GetConnectionString("FinIntelConnection")
    //         ?? throw new InvalidOperationException("Connection string 'FinIntelConnection' is missing.");

    //     var asOfDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

    //     var arguments =
    //         $"\"{scriptPath}\" " +
    //         $"--connectionString \"{connectionString}\" " +
    //         $"--businessId \"{businessId}\" " +
    //         $"--asOfDate \"{asOfDate}\" " +
    //         $"--monthsBack {monthsBack}";

    //     var startInfo = new ProcessStartInfo
    //     {
    //         FileName = pythonExe,
    //         Arguments = arguments,
    //         RedirectStandardOutput = true,
    //         RedirectStandardError = true,
    //         UseShellExecute = false,
    //         CreateNoWindow = true
    //     };

    //     using var process = new Process { StartInfo = startInfo };

    //     process.Start();

    //     var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
    //     var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

    //     await process.WaitForExitAsync(cancellationToken);

    //     var stdout = await stdoutTask;
    //     var stderr = await stderrTask;

    //     if (!string.IsNullOrWhiteSpace(stderr))
    //     {
    //         _logger.LogWarning("Python spend anomaly stderr: {Stderr}", stderr);
    //     }

    //     if (process.ExitCode != 0)
    //     {
    //         _logger.LogError(
    //             "Python spend anomaly failed. ExitCode={ExitCode}, Stdout={Stdout}, Stderr={Stderr}",
    //             process.ExitCode,
    //             stdout,
    //             stderr);

    //         return Array.Empty<InsightDto>();
    //     }

    //     PythonSpendAnomalyResponse? response;
    //     try
    //     {
    //         response = JsonSerializer.Deserialize<PythonSpendAnomalyResponse>(
    //             stdout,
    //             new JsonSerializerOptions
    //             {
    //                 PropertyNameCaseInsensitive = true
    //             });
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to deserialize Python spend anomaly response. Stdout={Stdout}", stdout);
    //         return Array.Empty<InsightDto>();
    //     }

    //     if (response is null)
    //     {
    //         _logger.LogWarning("Python spend anomaly returned null response.");
    //         return Array.Empty<InsightDto>();
    //     }

    //     if (!string.IsNullOrWhiteSpace(response.Error))
    //     {
    //         _logger.LogWarning(
    //             "Python spend anomaly returned an error for business {BusinessId}: {Error}",
    //             businessId,
    //             response.Error);

    //         return Array.Empty<InsightDto>();
    //     }

    //     if (response.Insights.Count == 0)
    //     {
    //         return Array.Empty<InsightDto>();
    //     }

    //     return response.Insights
    //         .Select(x => MapToInsightDto(businessId, x))
    //         .ToList();
    // }

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

    private static Dictionary<string, object> ConvertMetrics(Dictionary<string, object> metrics)
    {
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
            _ => element.ToString()
        };
    }

    private static InsightVisualizationDto? MapVisualization(PythonVisualizationDto? source)
    {
        if (source is null)
            return null;

        return new InsightVisualizationDto
        {
            Title = source.Title ?? "Monthly spend trend",
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