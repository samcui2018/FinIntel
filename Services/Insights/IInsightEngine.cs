using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services.Insights;

public interface IInsightEngine
{
    Task<IReadOnlyList<InsightRecord>> RunAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default);
}