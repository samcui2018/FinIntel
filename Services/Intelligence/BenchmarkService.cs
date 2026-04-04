using FinancialIntelligence.Api.Dtos.Intelligence;
using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services.Intelligence;

public sealed class BenchmarkService : IBenchmarkService
{
    private readonly IAnalyticsRepository _analyticsRepository;

    public BenchmarkService(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<BenchmarkComparisonDto> CompareAsync(
        Guid loadId,
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        var summary = await _analyticsRepository.GetSpendSummaryAsync(
            loadId,
            businessId,
            cancellationToken);

        var benchmark = GetStaticBenchmarkProfile();

        var monthlySpendDelta = summary.TotalAmount - benchmark.AvgMonthlySpend;
        var monthlySpendDeltaPct = benchmark.AvgMonthlySpend == 0
            ? 0
            : (monthlySpendDelta / benchmark.AvgMonthlySpend) * 100m;

        var avgTxnDelta = summary.AverageAmount - benchmark.AvgTransactionAmount;
        var avgTxnDeltaPct = benchmark.AvgTransactionAmount == 0
            ? 0
            : (avgTxnDelta / benchmark.AvgTransactionAmount) * 100m;

        var topVendorDeltaPct = summary.TopVendorPct - benchmark.TopVendorConcentrationPct;

        return new BenchmarkComparisonDto
        {
            Segment = benchmark.Segment,

            BusinessMonthlySpend = summary.TotalAmount,
            BenchmarkMonthlySpend = benchmark.AvgMonthlySpend,
            MonthlySpendDelta = Math.Round(monthlySpendDelta, 2),
            MonthlySpendDeltaPct = Math.Round(monthlySpendDeltaPct, 2),

            BusinessAverageTransaction = summary.AverageAmount,
            BenchmarkAverageTransaction = benchmark.AvgTransactionAmount,
            AverageTransactionDelta = Math.Round(avgTxnDelta, 2),
            AverageTransactionDeltaPct = Math.Round(avgTxnDeltaPct, 2),

            BusinessTopVendorConcentrationPct = summary.TopVendorPct,
            BenchmarkTopVendorConcentrationPct = benchmark.TopVendorConcentrationPct,
            TopVendorConcentrationDeltaPct = Math.Round(topVendorDeltaPct, 2),

            CurrencyCode = benchmark.CurrencyCode
        };
    }

    private static BenchmarkProfile GetStaticBenchmarkProfile()
    {
        return new BenchmarkProfile
        {
            Segment = "SMB-General",
            AvgMonthlySpend = 25000m,
            AvgTransactionAmount = 140m,
            TopVendorConcentrationPct = 35m,
            CurrencyCode = "USD"
        };
    }
}