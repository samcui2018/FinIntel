using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Services;

public interface IAnalyticsService
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
    Task<TopInsightsResponse> GetTopInsightsAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}