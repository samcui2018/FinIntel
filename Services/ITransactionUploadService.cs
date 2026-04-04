using FinancialIntelligence.Api.Dtos.Upload;

namespace FinancialIntelligence.Api.Services;

public interface ITransactionUploadService
{
    Task<UploadTransactionsResponse> UploadCsvAsync(
        IFormFile file,
        Guid businessId,
        Guid userId,
        CancellationToken cancellationToken = default);
}