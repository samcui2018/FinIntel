using FinancialIntelligence.Api.Domain.Transactions;
namespace FinancialIntelligence.Api.Models;

public sealed class TransactionNormalizationResult
{
    public decimal RawAmount { get; init; }
    public decimal SignedAmount { get; init; }
    public decimal AbsoluteAmount { get; init; }

    public EntryDirection EntryDirection { get; init; }
    public TransactionClass TransactionClass { get; init; }

    public bool CountsAsSpend { get; init; }
    public bool CountsAsRevenue { get; init; }
    public bool CountsAsTransfer { get; init; }
    public bool CountsAsDebtService { get; init; }

    public ConfidenceLevel DirectionConfidence { get; init; }
    public ConfidenceLevel ClassificationConfidence { get; init; }

    public string DirectionRuleApplied { get; init; } = default!;
    public string ClassificationRuleApplied { get; init; } = default!;
    public string? Notes { get; init; }
}