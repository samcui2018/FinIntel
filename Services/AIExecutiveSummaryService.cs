using System.Text;
using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Dtos.Intelligence;

namespace FinancialIntelligence.Api.Services;

public sealed class AiExecutiveSummaryService : IExecutiveSummaryService
{
    private readonly IGenerativeAiClient _aiClient;
    private readonly RuleBasedExecutiveSummaryService _fallback;

    public AiExecutiveSummaryService(
        IGenerativeAiClient aiClient,
        RuleBasedExecutiveSummaryService fallback)
    {
        _aiClient = aiClient;
        _fallback = fallback;
    }

    public async Task<string> GenerateAsync(
        Guid businessId,
        IReadOnlyList<InsightDto> insights,
        BenchmarkComparisonDto benchmark,
        SpendForecastDto forecast,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(insights, benchmark, forecast);

        try
        {
            var response = await _aiClient.GenerateAsync(prompt, cancellationToken);

            if (!string.IsNullOrWhiteSpace(response))
            {
                return response.Trim();
            }
        }
        catch
        {
            // optional: log exception
        }

        return await _fallback.GenerateAsync(
            businessId,
            insights,
            benchmark,
            forecast,
            cancellationToken);
    }

    private static string BuildPrompt(
        IReadOnlyList<InsightDto> insights,
        BenchmarkComparisonDto benchmark,
        SpendForecastDto forecast)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a financial analyst writing a concise executive summary for a small business owner.");
        sb.AppendLine("Write one short paragraph in plain English.");
        sb.AppendLine("Be specific, practical, and avoid jargon.");
        sb.AppendLine("Do not invent facts.");
        sb.AppendLine("Use only the structured information below.");
        sb.AppendLine();

        sb.AppendLine("Top insights:");
        if (insights.Count == 0)
        {
            sb.AppendLine("- No high-confidence insights available.");
        }
        else
        {
            foreach (var insight in insights
                .OrderByDescending(x => x.Score)
                .Take(5))
            {
                sb.AppendLine(
                    $"- [{insight.Severity}] {insight.Title}: {insight.Description} " +
                    $"Impact={(insight.EstimatedImpact?.ToString("C") ?? "n/a")}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Benchmark comparison:");
        sb.AppendLine($"- Segment: {benchmark.Segment}");
        sb.AppendLine($"- Monthly spend delta: {benchmark.MonthlySpendDeltaPct:0.##}%");
        sb.AppendLine($"- Transaction size delta: {benchmark.AverageTransactionDeltaPct:0.##}%");
        sb.AppendLine($"- Top vendor concentration delta: {benchmark.TopVendorConcentrationDeltaPct:0.##}%");

        sb.AppendLine();
        sb.AppendLine("Forecast:");
        sb.AppendLine($"- Has sufficient history: {forecast.HasSufficientHistory}");
        sb.AppendLine($"- Next month forecast: {forecast.NextMonthForecast:0.##}");
        sb.AppendLine($"- Trend slope: {forecast.TrendSlope:0.##}");

        sb.AppendLine();
        sb.AppendLine("Instructions:");
        sb.AppendLine("- Mention the most important risk or opportunity first.");
        sb.AppendLine("- Mention benchmark position if materially above or below benchmark.");
        sb.AppendLine("- Mention the forecast only if sufficient history exists.");
        sb.AppendLine("- End with what management should review next.");

        return sb.ToString();
    }
}