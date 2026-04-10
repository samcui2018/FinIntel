using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services;

public interface IInsightGenerator
{
    Task<IReadOnlyList<InsightRecord>> GenerateAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default);
}