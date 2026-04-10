using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Repositories;

public interface ITransactionRepository
{
    Task<int> StageTransactionsAsync(
        IReadOnlyList<CanonicalTransaction> transactions,
        CancellationToken cancellationToken = default);

    Task<(int InsertedCount, List<CanonicalTransaction> InsertedRows)> PromoteTransactionsAsync(
        Guid loadId,
        CancellationToken cancellationToken = default);

    Task CreateUploadRecordAsync(
        Guid loadId,
        Guid businessId,
        string sourceType,
        string sourceName,
        int rowsInFile,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
    Task UpdateUploadStatusAsync(
        Guid loadId,
        string status,
        int? rowsInserted = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    // Task CreateAnalyticsJobAsync(
    //     Guid loadId,
    //     string businessId,
    //     CancellationToken cancellationToken = default);

    Task<(int IngestionDuplicates, int PossibleDuplicateCharges)> GetLoadDuplicateSummaryAsync(
        Guid loadId,
        CancellationToken cancellationToken = default);
}