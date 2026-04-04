using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Repositories;

public interface IAnalyticsRepository
{
    Task<IReadOnlyList<UploadHistoryItemResponse>> GetUploadHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AnalyticsSummaryDto> GetSpendSummaryAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyTrendPointResponse>> GetMonthlySpendTrendAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopMerchantResponse>> GetTopSpendMerchantsAsync(
        Guid userId,
        Guid businessId,
        int top,
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