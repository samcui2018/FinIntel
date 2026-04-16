using FinancialIntelligence.Api.Dtos.Intelligence;
namespace FinancialIntelligence.Api.Models.Intelligence;

public sealed class InterchangeOptimizationSnapshot
{
    public EcommerceOptimizationCandidate? Ecommerce { get; set; }
    public Level23OptimizationCandidate? Level23 { get; set; }
    public SmallTicketOptimizationCandidate? SmallTicket { get; set; }
    public ManualEntryOptimizationCandidate? ManualEntry { get; set; }
}

public sealed class EcommerceOptimizationCandidate
{
    public int TotalTransactionCount { get; set; }
    public decimal TotalVolume { get; set; }
    public int AffectedTransactionCount { get; set; }
    public decimal AffectedVolume { get; set; }
    public string TopMerchantsCsv { get; set; } = string.Empty;
    public IReadOnlyList<ChannelBreakdownDto> ChannelBreakdown { get; set; } = Array.Empty<ChannelBreakdownDto>();
}

public sealed class Level23OptimizationCandidate
{
    public int TotalTransactionCount { get; set; }
    public decimal TotalVolume { get; set; }
    public int AffectedTransactionCount { get; set; }
    public decimal AffectedVolume { get; set; }
    public string TopMerchantsCsv { get; set; } = string.Empty;
    public IReadOnlyList<ChannelBreakdownDto> ChannelBreakdown { get; set; } = Array.Empty<ChannelBreakdownDto>();
}

public sealed class SmallTicketOptimizationCandidate
{
    public int TotalTransactionCount { get; set; }
    public decimal TotalVolume { get; set; }
    public int AffectedTransactionCount { get; set; }
    public decimal AffectedVolume { get; set; }
    public decimal ThresholdAmount { get; set; }
    public string TopMerchantsCsv { get; set; } = string.Empty;
    public IReadOnlyList<ChannelBreakdownDto> ChannelBreakdown { get; set; } = Array.Empty<ChannelBreakdownDto>();
}

public sealed class ManualEntryOptimizationCandidate
{
    public int TotalTransactionCount { get; set; }
    public decimal TotalVolume { get; set; }
    public int AffectedTransactionCount { get; set; }
    public decimal AffectedVolume { get; set; }
    public string TopMerchantsCsv { get; set; } = string.Empty;
    public IReadOnlyList<ChannelBreakdownDto> ChannelBreakdown { get; set; } = Array.Empty<ChannelBreakdownDto>();
}