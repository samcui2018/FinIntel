using FinancialIntelligence.Api.Models.Intelligence;

namespace FinancialIntelligence.Api.Repositories;

public interface IInterchangeOptimizationRepository
{
    Task<InterchangeOptimizationSnapshot> GetSnapshotAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}