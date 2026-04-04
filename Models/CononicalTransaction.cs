namespace FinancialIntelligence.Api.Models;

public sealed class CanonicalTransaction
{
    public Guid TransactionId { get; set; }
    public Guid LoadId { get; set; }
    public Guid BusinessId { get; set; } = Guid.Empty;
    public int BusinessKey { get; set; }
    public string SourceFile { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string SourceName { get; set; } = "";
    public int SourceRowNumber { get; set; }
    public string? MerchantAccountId { get; set; }
    public string? CardAccountId { get; set; }
    public string? StatementId { get; set; }
    public string? SourceTransactionId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? AuthorizationCode { get; set; }

    public DateTime TransactionDate { get; set; }
    public DateTime? PostedDate { get; set; }
    public DateTime? TransactionDateTime { get; set; }

    public decimal Amount { get; set; }
    public string? CurrencyCode { get; set; }

    public string? MerchantName { get; set; }
    public string? NormalizedMerchantName { get; set; }
    public string? Description { get; set; }

    public string DedupeKey { get; set; } = "";
    public string RawPayloadJson { get; set; } = "";

    public bool IsPossibleDuplicateCharge { get; set; }
    public string? DuplicateReason { get; set; }

    public bool IsIngestionDuplicate { get; set; }
    public byte[]? IngestionDedupeKey { get; set; }

    public byte[]? PossibleDuplicateChargeKey { get; set; }
}
// public sealed class CanonicalTransaction
// {
//     public Guid LoadId { get; set; }

//     public int BusinessKey { get; set; }
//     public string BusinessId { get; set; } = "";

//     public string SourceType { get; set; } = "";
//     public string SourceName { get; set; } = "";
//     public int SourceRowNumber { get; set; }

//     public string? MerchantAccountId { get; set; }
//     public string? CardAccountId { get; set; }
//     public string? StatementId { get; set; }
//     public string? SourceTransactionId { get; set; }
//     public string? ReferenceNumber { get; set; }
//     public string? AuthorizationCode { get; set; }

//     public DateTime TransactionDate { get; set; }
//     public DateTime? PostedDate { get; set; }
//     public DateTime? TransactionDateTime { get; set; }

//     public decimal Amount { get; set; }
//     public string CurrencyCode { get; set; } = "USD";

//     public string MerchantName { get; set; } = "";
//     public string NormalizedMerchantName { get; set; } = "";
//     public string Description { get; set; } = "";

//     public byte[]? IngestionDedupeKey { get; set; }
//     public bool IsIngestionDuplicate { get; set; }

//     public byte[]? PossibleDuplicateChargeKey { get; set; }
//     public bool IsPossibleDuplicateCharge { get; set; }

//     public string? DuplicateReason { get; set; }
// }