using System.Globalization;
using FinancialIntelligence.Api.Domain.Transactions;
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services;

public sealed class TransactionNormalizer : ITransactionNormalizer
{
    private static readonly string[] CardPaymentKeywords =
    [
        "PAYMENT THANK YOU",
        "CARD PAYMENT",
        "CREDIT CARD PAYMENT",
        "EPAYMENT",
        "AUTOPAY PAYMENT"
    ];

    private static readonly string[] RefundKeywords =
    [
        "REFUND",
        "REVERSAL",
        "RETURN"
    ];

    private static readonly string[] TransferKeywords =
    [
        "TRANSFER",
        "XFER",
        "INTERNAL TRANSFER"
    ];

    private static readonly string[] FeeKeywords =
    [
        "FEE",
        "SERVICE CHARGE",
        "MONTHLY FEE"
    ];

    private static readonly string[] PayoutKeywords =
    [
        "PAYOUT",
        "STRIPE PAYOUT",
        "PAYPAL TRANSFER",
        "SQUARE TRANSFER"
    ];

    public TransactionNormalizationResult Normalize(TransactionNormalizationContext context)
    {
        var directionResult = DetermineDirection(context);
        var classificationResult = DetermineClassification(context, directionResult.EntryDirection);

        return new TransactionNormalizationResult
        {
            RawAmount = context.RawAmount ?? 0m,
            SignedAmount = NormalizeSignedAmount(context, directionResult.EntryDirection),
            AbsoluteAmount = Math.Abs(context.RawAmount ?? 0m),

            EntryDirection = directionResult.EntryDirection,
            TransactionClass = classificationResult.TransactionClass,

            CountsAsSpend = directionResult.EntryDirection == EntryDirection.Debit &&
                            classificationResult.TransactionClass is TransactionClass.Expense or TransactionClass.Fee or TransactionClass.Payroll,

            CountsAsRevenue = directionResult.EntryDirection == EntryDirection.Credit &&
                              classificationResult.TransactionClass is TransactionClass.Income or TransactionClass.Payout,

            CountsAsTransfer = classificationResult.TransactionClass == TransactionClass.Transfer,
            CountsAsDebtService = classificationResult.TransactionClass is TransactionClass.CardPayment or TransactionClass.LoanPayment,

            DirectionConfidence = directionResult.Confidence,
            ClassificationConfidence = classificationResult.Confidence,
            DirectionRuleApplied = directionResult.RuleApplied,
            ClassificationRuleApplied = classificationResult.RuleApplied,
            Notes = classificationResult.Notes
        };
    }

    private static (EntryDirection EntryDirection, ConfidenceLevel Confidence, string RuleApplied) DetermineDirection(
        TransactionNormalizationContext context)
    {
        if (context.SourceProfile.HasSeparateDebitCreditColumns)
        {
            if ((context.RawDebitAmount ?? 0m) > 0m)
            {
                return (EntryDirection.Debit, ConfidenceLevel.High, "SeparateDebitCreditColumns:DebitColumn");
            }

            if ((context.RawCreditAmount ?? 0m) > 0m)
            {
                return (EntryDirection.Credit, ConfidenceLevel.High, "SeparateDebitCreditColumns:CreditColumn");
            }
        }

        if (!string.IsNullOrWhiteSpace(context.RawDebitCreditIndicator))
        {
            var dc = context.RawDebitCreditIndicator.Trim().ToUpperInvariant();

            if (dc is "D" or "DB" or "DEBIT" or "DR")
            {
                return (EntryDirection.Debit, ConfidenceLevel.High, "RawDebitCreditIndicator");
            }

            if (dc is "C" or "CR" or "CREDIT")
            {
                return (EntryDirection.Credit, ConfidenceLevel.High, "RawDebitCreditIndicator");
            }
        }

        var rawAmount = context.RawAmount ?? 0m;

        switch (context.SourceProfile.SignConvention)
        {
            case SignConvention.NegativeIsDebit:
                if (rawAmount < 0m) return (EntryDirection.Debit, ConfidenceLevel.High, "SignConvention:NegativeIsDebit");
                if (rawAmount > 0m) return (EntryDirection.Credit, ConfidenceLevel.High, "SignConvention:NegativeIsDebit");
                break;

            case SignConvention.PositiveIsDebit:
                if (rawAmount > 0m) return (EntryDirection.Debit, ConfidenceLevel.High, "SignConvention:PositiveIsDebit");
                if (rawAmount < 0m) return (EntryDirection.Credit, ConfidenceLevel.High, "SignConvention:PositiveIsDebit");
                break;
        }

        var haystack = $"{context.Description} {context.MerchantName} {context.RawTransactionType}".ToUpperInvariant();

        if (ContainsAny(haystack, PayoutKeywords))
        {
            return (EntryDirection.Credit, ConfidenceLevel.Medium, "Keyword:Payout");
        }

        if (ContainsAny(haystack, RefundKeywords))
        {
            return (EntryDirection.Credit, ConfidenceLevel.Medium, "Keyword:Refund");
        }

        if (ContainsAny(haystack, CardPaymentKeywords) || ContainsAny(haystack, FeeKeywords))
        {
            return (EntryDirection.Debit, ConfidenceLevel.Medium, "Keyword:DebitLike");
        }

        return (EntryDirection.Unknown, ConfidenceLevel.Low, "Unknown");
    }

    private static (TransactionClass TransactionClass, ConfidenceLevel Confidence, string RuleApplied, string? Notes)
        DetermineClassification(TransactionNormalizationContext context, EntryDirection direction)
    {
        var haystack = $"{context.Description} {context.MerchantName} {context.RawTransactionType}".ToUpperInvariant();

        if (ContainsAny(haystack, CardPaymentKeywords))
        {
            return (TransactionClass.CardPayment, ConfidenceLevel.High, "Keyword:CardPayment", "Detected payment-to-card-issuer pattern.");
        }

        if (ContainsAny(haystack, RefundKeywords))
        {
            return (TransactionClass.Refund, ConfidenceLevel.High, "Keyword:Refund", null);
        }

        if (ContainsAny(haystack, TransferKeywords))
        {
            return (TransactionClass.Transfer, ConfidenceLevel.High, "Keyword:Transfer", null);
        }

        if (ContainsAny(haystack, FeeKeywords))
        {
            return (TransactionClass.Fee, ConfidenceLevel.High, "Keyword:Fee", null);
        }

        if (ContainsAny(haystack, PayoutKeywords))
        {
            return (TransactionClass.Payout, ConfidenceLevel.High, "Keyword:Payout", null);
        }

        if (direction == EntryDirection.Debit)
        {
            return (TransactionClass.Expense, ConfidenceLevel.Medium, "Fallback:Debit=>Expense", null);
        }

        if (direction == EntryDirection.Credit)
        {
            return (TransactionClass.Income, ConfidenceLevel.Medium, "Fallback:Credit=>Income", null);
        }

        return (TransactionClass.Unknown, ConfidenceLevel.Low, "Unknown", "Could not confidently classify transaction.");
    }

    private static decimal NormalizeSignedAmount(TransactionNormalizationContext context, EntryDirection direction)
    {
        var amount = Math.Abs(context.RawAmount ?? 0m);

        return direction switch
        {
            EntryDirection.Debit => -amount,
            EntryDirection.Credit => amount,
            _ => context.RawAmount ?? 0m
        };
    }

    private static bool ContainsAny(string input, IEnumerable<string> values)
        => values.Any(v => input.Contains(v, StringComparison.OrdinalIgnoreCase));
}