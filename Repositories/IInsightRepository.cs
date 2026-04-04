using FinancialIntelligence.Api.Models;
using Microsoft.Data.SqlClient;

namespace FinancialIntelligence.Api.Repositories;

public interface IInsightRepository
{
    Task BuildInsightsAsync(
        // Guid loadId,
        // Guid businessId,
        IReadOnlyList<InsightRecord> insights,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InsightRecord>> GetByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InsightRecord>> GetByLoadIdAsync(
        Guid loadId,
        CancellationToken cancellationToken = default);
}