using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Repositories;

public interface IBenchmarkRepository
{
    Task<BusinessBenchmarkContext?> GetBusinessBenchmarkContextAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BenchmarkProfile>> GetBenchmarkProfilesAsync(
        CancellationToken cancellationToken = default);

    Task<BenchmarkMetricValue?> GetBenchmarkMetricValueAsync(
        Guid benchmarkProfileId,
        string metricKey,
        CancellationToken cancellationToken = default);
}