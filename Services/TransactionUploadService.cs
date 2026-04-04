using FinancialIntelligence.Api.Adapters;
using FinancialIntelligence.Api.Dtos.Upload;
using FinancialIntelligence.Api.Repositories;
using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Services.Insights;

namespace FinancialIntelligence.Api.Services;

public sealed class TransactionUploadService : ITransactionUploadService
{
    private readonly ICsvSourceAdapter _csvSourceAdapter;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IInsightEngine _insightEngine;

    public TransactionUploadService(
        ICsvSourceAdapter csvSourceAdapter,
        ITransactionRepository transactionRepository,
        IUserRepository userRepository,
        IInsightEngine insightEngine)
    {
        _csvSourceAdapter = csvSourceAdapter;
        _transactionRepository = transactionRepository;
        _userRepository = userRepository;
        _insightEngine = insightEngine;
    }

    public async Task<UploadTransactionsResponse> UploadCsvAsync(
    IFormFile file,
    Guid businessId,
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File is empty.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only CSV files are supported.");

        var businessKey = await _userRepository.GetBusinessKeyForUserAsync(userId, cancellationToken);

        var loadId = Guid.NewGuid();

        List<CanonicalTransaction> rows;
        await using (var stream = file.OpenReadStream())
        {
            rows = _csvSourceAdapter
                .Parse(stream, businessId, file.FileName, loadId)
                .ToList();
        }

        await _transactionRepository.CreateUploadRecordAsync(
            loadId,
            businessId,
            "CSV",
            file.FileName,
            rows.Count,
            userId,
            cancellationToken);

        try
        {
            var rowsStaged = await _transactionRepository.StageTransactionsAsync(rows, cancellationToken);
            var promoted = await _transactionRepository.PromoteTransactionsAsync(loadId, cancellationToken);
            var summary = await _transactionRepository.GetLoadDuplicateSummaryAsync(loadId, cancellationToken);

            var insights = await _insightEngine.RunAsync(loadId, businessId, cancellationToken);
            //var InsightCount = insights.Count;

            await _transactionRepository.UpdateUploadStatusAsync(
                loadId,
                "Completed",
                promoted.InsertedCount,
                null,
                cancellationToken);

            return new UploadTransactionsResponse
            {
                LoadId = loadId,
                RowsInFile = rows.Count,
                RowsStaged = rowsStaged,
                IngestionDuplicates = summary.IngestionDuplicates,
                PossibleDuplicateCharges = summary.PossibleDuplicateCharges,
                RowsPromoted = promoted.InsertedCount,
                Status = "Completed",
                InsightCount = insights.Count
            };
        }
        catch (Exception ex)
        {
            await _transactionRepository.UpdateUploadStatusAsync(
                loadId,
                "Failed",
                null,
                ex.Message,
                cancellationToken);

            throw;
        }
    }

}