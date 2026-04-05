// namespace FinancialIntelligence.Api.Dtos.Analytics;

// public sealed class TopInsightsResponse
// {
//     public Guid BusinessId { get; set; } = Guid.Empty;
//     public DateTime GeneratedAtUtc { get; set; }
//     public int LookbackMonths { get; set; }
//     public IReadOnlyList<InsightDto> Insights { get; set; } = new();
// }
namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class TopInsightsResponse
{
    public Guid BusinessId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public int LookbackMonths { get; set; }
    public IReadOnlyList<InsightDto> Insights { get; set; } = Array.Empty<InsightDto>();
}
