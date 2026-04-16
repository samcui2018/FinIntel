
using FinancialIntelligence.Api.Domain.Transactions;
namespace FinancialIntelligence.Api.Models;

public sealed class CanonicalTransaction
{
    public Guid TransactionId { get; set; }
    public Guid LoadId { get; set; }
    public Guid BusinessId { get; set; }
    public int BusinessKey { get; set; }

    public string? MerchantAccountId { get; set; }
    public string? CardAccountId { get; set; }
    public string? StatementId { get; set; }
    public string? SourceTransactionId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? AuthorizationCode { get; set; }

    public DateTime TransactionDate { get; set; }
    public DateTime? PostedDate { get; set; }
    public DateTime? TransactionDateTime { get; set; }

    public decimal Amount { get; set; }          // backward compatibility
    public decimal RawAmount { get; set; }
    public decimal SignedAmount { get; set; }
    public decimal AbsoluteAmount { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public string? MerchantName { get; set; }
    public string? NormalizedMerchantName { get; set; }
    public string? Description { get; set; }
    public string? Channel { get; set; }
    public EntryDirection EntryDirection { get; set; }
    public TransactionClass TransactionClass { get; set; }

    public bool CountsAsSpend { get; set; }
    public bool CountsAsRevenue { get; set; }
    public bool CountsAsTransfer { get; set; }
    public bool CountsAsDebtService { get; set; }

    public string? RawTransactionType { get; set; }
    public string? RawDebitCreditIndicator { get; set; }
    public string? SourceProfileId { get; set; }

    public ConfidenceLevel DirectionConfidence { get; set; }
    public ConfidenceLevel ClassificationConfidence { get; set; }

    public string? DirectionRuleApplied { get; set; }
    public string? ClassificationRuleApplied { get; set; }
    public string? NormalizationNotes { get; set; }

    public bool IsPossibleDuplicateCharge { get; set; }
    public string? DuplicateReason { get; set; }

    public string? SourceFile { get; set; }
    public string? SourceType { get; set; }
    public string? SourceName { get; set; }
    public int SourceRowNumber { get; set; }

    public string DedupeKey { get; set; } = "";
    public string RawPayloadJson { get; set; } = "";
    public byte[] IngestionDedupeKey { get; set; }
    public Boolean IsIngestionDuplicate { get; set; }
    public byte[] PossibleDuplicateChargeKey { get; set; }
}
