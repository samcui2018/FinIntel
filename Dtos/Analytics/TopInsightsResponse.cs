namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class TopInsightsResponse
{
    public Guid BusinessId { get; set; } = Guid.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public int LookbackMonths { get; set; }
    public List<InsightDto> Insights { get; set; } = new();
}
