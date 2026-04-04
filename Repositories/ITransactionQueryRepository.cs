using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Repositories;

public interface ITransactionQueryRepository
{
    Task<IReadOnlyList<TransactionRecord>> GetByLoadIdAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionRecord>> GetRecentTransactionsForBusinessAsync(
        Guid businessId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}