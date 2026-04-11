using FinancialIntelligence.Api.Dtos.Upload;

namespace FinancialIntelligence.Api.Services;

public interface ITransactionUploadService
{
    Task<UploadTransactionsResponse> UploadFileAsync(
        IFormFile file,
        Guid businessId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UploadTransactionsResponse> UploadCsvAsync(
        IFormFile file,
        Guid businessId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

// using FinancialIntelligence.Api.Dtos.Upload;

// namespace FinancialIntelligence.Api.Services;

// public interface ITransactionUploadService
// {
//     Task<UploadTransactionsResponse> UploadCsvAsync(
//         IFormFile file,
//         Guid businessId,
//         Guid userId,
//         CancellationToken cancellationToken = default);
// }