using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Utilities;

public static class TransactionKeyBuilder
{
    public static void Enrich(CanonicalTransaction tx)
    {
        tx.NormalizedMerchantName = NormalizeMerchantName(tx.MerchantName);

        tx.IngestionDedupeKey = BuildIngestionDedupeKey(tx);
        tx.PossibleDuplicateChargeKey = BuildPossibleDuplicateChargeKey(tx);

        tx.IsIngestionDuplicate = false;
        tx.IsPossibleDuplicateCharge = false;
        tx.DuplicateReason = null;
    }

    public static byte[] BuildIngestionDedupeKey(CanonicalTransaction tx)
    {
        string raw;

        if (!string.IsNullOrWhiteSpace(tx.SourceTransactionId))
        {
            raw = string.Join("|",
                tx.BusinessKey.ToString(CultureInfo.InvariantCulture),
                Normalize(tx.SourceType),
                Normalize(tx.SourceName),
                Normalize(tx.StatementId),
                Normalize(tx.SourceTransactionId));
        }
        else if (!string.IsNullOrWhiteSpace(tx.ReferenceNumber) ||
                 !string.IsNullOrWhiteSpace(tx.AuthorizationCode))
        {
            raw = string.Join("|",
                tx.BusinessKey.ToString(CultureInfo.InvariantCulture),
                Normalize(tx.SourceType),
                Normalize(tx.SourceName),
                Normalize(tx.StatementId),
                Normalize(tx.ReferenceNumber),
                Normalize(tx.AuthorizationCode),
                tx.SourceRowNumber.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            raw = string.Join("|",
                tx.BusinessKey.ToString(CultureInfo.InvariantCulture),
                Normalize(tx.SourceType),
                Normalize(tx.SourceName),
                Normalize(tx.StatementId),
                Normalize(tx.MerchantAccountId),
                Normalize(tx.CardAccountId),
                tx.SourceRowNumber.ToString(CultureInfo.InvariantCulture));
        }

        return Sha256(raw);
    }

    public static byte[] BuildPossibleDuplicateChargeKey(CanonicalTransaction tx)
    {
        var raw = string.Join("|",
            tx.BusinessKey.ToString(CultureInfo.InvariantCulture),
            Normalize(tx.MerchantAccountId),
            Normalize(tx.CardAccountId),
            Normalize(tx.NormalizedMerchantName),
            tx.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            tx.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            Normalize(tx.CurrencyCode));

        return Sha256(raw);
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToUpperInvariant();

    private static string NormalizeMerchantName(string? merchantName)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
            return "";

        var s = merchantName.Trim().ToUpperInvariant()
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace("-", " ")
            .Replace("/", " ");

        while (s.Contains("  "))
            s = s.Replace("  ", " ");

        return s;
    }

    private static byte[] Sha256(string input)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
    }
}