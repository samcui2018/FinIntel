using FinancialIntelligence.Api.Domain.Transactions;

namespace FinancialIntelligence.Api.Models;

public sealed class SourceProfile
{
    public string SourceProfileId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string SourceType { get; set; } = default!;

    public SignConvention SignConvention { get; set; }

    public bool HasSeparateDebitCreditColumns { get; set; }
    public string? AmountColumnName { get; set; }
    public string? DebitColumnName { get; set; }
    public string? CreditColumnName { get; set; }
    public string? DescriptionColumnName { get; set; }
    public string? TransactionTypeColumnName { get; set; }
    public string? DebitCreditIndicatorColumnName { get; set; }

    public bool UseDescriptionHeuristics { get; set; }
}