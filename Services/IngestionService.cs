using FinancialIntelligence.Api.Adapters;
using FinancialIntelligence.Api.Dtos.Upload;
using FinancialIntelligence.Api.Repositories;
using FinancialIntelligence.Api.Services.Insights;

namespace FinancialIntelligence.Api.Services;

public sealed class IngestionService
{
    private readonly IEnumerable<ISourceAdapter> _adapters;
    private readonly ITransactionRepository _repository;
    private readonly IInsightEngine _insightEngine;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        IEnumerable<ISourceAdapter> adapters,
        ITransactionRepository repository,
        IInsightEngine insightEngine,
        ILogger<IngestionService> logger)
    {
        _adapters = adapters;
        _repository = repository;
        _insightEngine = insightEngine;
        _logger = logger;
    }

    public async Task<UploadCsvResponse> IngestAsync(
        Stream fileStream,
        string sourceName,
        string sourceType,
        Guid businessId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var loadId = Guid.NewGuid();

        var adapter = _adapters.FirstOrDefault(x =>
            string.Equals(x.SourceType, sourceType, StringComparison.OrdinalIgnoreCase));

        if (adapter is null)
        {
            throw new InvalidOperationException($"No adapter registered for source type '{sourceType}'.");
        }

        var transactions = await adapter.ParseAsync(
            loadId: loadId,
            fileStream: fileStream,
            sourceName: sourceName,
            businessId: businessId,
            createdByUserId: createdByUserId,
            cancellationToken: cancellationToken);

        await _repository.CreateUploadRecordAsync(
            loadId: loadId,
            createdByUserId: createdByUserId,
            businessId: businessId,
            sourceType: sourceType,
            sourceName: sourceName,
            rowsInFile: transactions.Count,
            cancellationToken: cancellationToken);

        try
        {
            var rowsStaged = await _repository.StageTransactionsAsync(
                transactions,
                cancellationToken);

            var promoteResult = await _repository.PromoteTransactionsAsync(
                loadId,
                cancellationToken);

            int insightCount = 0;

            try
            {
                var insights = await _insightEngine.RunAsync(
                    loadId,
                    businessId,
                    cancellationToken);

                insightCount = insights.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Insight generation failed for LoadId {LoadId} and BusinessId {BusinessId}",
                    loadId,
                    businessId);
            }

            await _repository.UpdateUploadStatusAsync(
                loadId: loadId,
                status: "Completed",
                rowsInserted: promoteResult.InsertedCount,
                cancellationToken: cancellationToken);

            return new UploadCsvResponse
            {
                Message = "Upload processed successfully.",
                LoadId = loadId,
                BusinessId = businessId,
                RowsInFile = transactions.Count,
                RowsStaged = rowsStaged,
                RowsInserted = promoteResult.InsertedCount,
                RowsSkippedAsDuplicates = transactions.Count - promoteResult.InsertedCount,
                Status = insightCount >= 0 ? "Completed" : "Promoted",
                InsightCount = insightCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Upload ingestion failed for LoadId {LoadId} and BusinessId {BusinessId}",
                loadId,
                businessId);

            await _repository.UpdateUploadStatusAsync(
                loadId: loadId,
                status: "Failed",
                cancellationToken: cancellationToken);

            throw;
        }
    }
}