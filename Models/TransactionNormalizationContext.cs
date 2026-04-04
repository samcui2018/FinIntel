namespace FinancialIntelligence.Api.Models;

public interface ITransactionNormalizer
{
    TransactionNormalizationResult Normalize(
        TransactionNormalizationContext context);
}

public sealed class TransactionNormalizationContext
{
    public SourceProfile SourceProfile { get; init; } = default!;
    public decimal? RawAmount { get; init; }
    public decimal? RawDebitAmount { get; init; }
    public decimal? RawCreditAmount { get; init; }

    public string? Description { get; init; }
    public string? MerchantName { get; init; }
    public string? RawTransactionType { get; init; }
    public string? RawDebitCreditIndicator { get; init; }
}