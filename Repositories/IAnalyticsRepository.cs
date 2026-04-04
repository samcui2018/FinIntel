using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Repositories;

public interface IAnalyticsRepository
{
    Task<AnalyticsSummaryDto> GetSummaryAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyTrendPointResponse>> GetMonthlyTrendAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopMerchantResponse>> GetTopMerchantsAsync(
        Guid userId,
        Guid businessId,
        int top,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UploadHistoryItemResponse>> GetUploadHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlySpendDto>> GetMonthlySpendAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopMerchantDto>> GetTopMerchantSpendAsync(
        Guid businessId,
        int monthsBack,
        int topN,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategorySpendDto>> GetMonthlyCategorySpendAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DuplicateChargeDto>> GetPossibleDuplicateChargesAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}