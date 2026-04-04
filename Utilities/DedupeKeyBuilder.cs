using System.Security.Cryptography;
using System.Text;
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Utilities;

// Minimal CanonicalTransaction model to satisfy references.
// Adjust or move this type to a shared Models namespace if you already have one.

public static class DedupeKeyBuilder
{
    public static string Build(
        Guid businessId,
        DateTime txnDate,
        decimal amount,
        string description,
        string? externalTransactionId)
    {
        var raw = !string.IsNullOrWhiteSpace(externalTransactionId)
            ? $"{businessId}|ext|{externalTransactionId.Trim()}"
            : $"{businessId}|{txnDate:yyyy-MM-dd}|{amount:F2}|{description.Trim().ToLowerInvariant()}";

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    public static byte[] ComputeDedupeKey(CanonicalTransaction txn)
    {
        if (txn == null)
            throw new ArgumentNullException(nameof(txn));

        // Normalize fields to avoid false mismatches
        var businessId = txn.BusinessId;
        var date = txn.TransactionDate.ToString("yyyy-MM-dd"); // fixed format
        var amount = txn.Amount.ToString("F2"); // ensures 2 decimal places
        var description = Normalize(txn.Description);
        var merchant = Normalize(txn.MerchantName);

        var raw = $"{businessId}|{date}|{amount}|{description}|{merchant}";

        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        return input.Trim().ToLowerInvariant();
    }
}