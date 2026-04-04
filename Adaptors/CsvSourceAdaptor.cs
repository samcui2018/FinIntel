using CsvHelper;
using CsvHelper.Configuration;
using FinancialIntelligence.Api.Models;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FinancialIntelligence.Api.Adapters;

public sealed class CsvSourceAdapter : ICsvSourceAdapter
{
    private static readonly Dictionary<string, string[]> ColumnAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["transaction_date"] = new[]
            {
                "transaction_date", "date", "txn_date", "trans_date",
                "purchase_date", "transactiondate"
            },
            ["posted_date"] = new[]
            {
                "posted_date", "post_date", "posting_date",
                "settlement_date", "posteddate"
            },
            ["amount"] = new[]
            {
                "amount", "transaction_amount", "amt", "net_amount"
            },
            ["debit_amount"] = new[]
            {
                "debit", "debit_amount", "withdrawal", "withdrawal_amount"
            },
            ["credit_amount"] = new[]
            {
                "credit", "credit_amount", "deposit", "deposit_amount"
            },
            ["merchant_name"] = new[]
            {
                "merchant_name", "merchant", "vendor", "vendor_name",
                "payee", "merchantname"
            },
            ["description"] = new[]
            {
                "description", "memo", "details", "transaction_description",
                "memo_text", "desc"
            },
            ["reference_number"] = new[]
            {
                "reference_number", "reference", "ref_number",
                "trace_number", "trace_no", "auth_code"
            },
            ["currency_code"] = new[]
            {
                "currency_code", "currency", "curr", "currencycode"
            },
            ["source_transaction_id"] = new[]
            {
                "source_transaction_id", "transaction_id", "txn_id", "source_id"
            },
            ["merchant_account_id"] = new[]
            {
                "merchant_account_id", "merchantaccountid", "mid"
            },
            ["card_account_id"] = new[]
            {
                "card_account_id", "cardaccountid", "card_last4", "last4"
            },
            ["statement_id"] = new[]
            {
                "statement_id", "statementid", "statement_number"
            },
            ["reference_number_alt"] = new[]
            {
                "rrn", "retrieval_reference_number"
            },
            ["authorization_code"] = new[]
            {
                "authorization_code", "authcode", "approval_code"
            },
            ["merchant_name_alt"] = new[]
            {
                "merchant_description", "merchant_desc"
            }
        };

    public IEnumerable<CanonicalTransaction> Parse(
        Stream stream,
        Guid businessId,
        string sourceFile,
        Guid loadId)
    {
        using var reader = new StreamReader(stream);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        };

        using var csv = new CsvReader(reader, config);

        if (!csv.Read())
            yield break;

        csv.ReadHeader();
        var rawHeaders = csv.HeaderRecord ?? Array.Empty<string>();

        var normalizedHeaders = rawHeaders
            .Select(NormalizeColumnName)
            .ToArray();

        var sourceRowNumber = 1;

        while (csv.Read())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < rawHeaders.Length; i++)
            {
                var rawHeader = rawHeaders[i];
                var normalizedHeader = normalizedHeaders[i];
                row[normalizedHeader] = csv.GetField(rawHeader)?.Trim() ?? string.Empty;
            }

            var transaction = MapRow(row, businessId, sourceFile, loadId);

            if (transaction != null)
            {
                transaction.SourceRowNumber = sourceRowNumber;
                yield return transaction;
            }

            sourceRowNumber++;
        }
    }

    private static CanonicalTransaction? MapRow(
        Dictionary<string, string> row,
        Guid businessId,
        string sourceFile,
        Guid loadId)
    {
        var transactionDateRaw = GetFirstValue(row, "transaction_date");
        var postedDateRaw = GetFirstValue(row, "posted_date");

        var transactionDate = ParseDate(transactionDateRaw) ?? ParseDate(postedDateRaw);
        var postedDate = ParseDate(postedDateRaw);

        var amount = ParseAmount(GetFirstValue(row, "amount"));

        if (!amount.HasValue)
        {
            var debit = ParseAmount(GetFirstValue(row, "debit_amount"));
            var credit = ParseAmount(GetFirstValue(row, "credit_amount"));

            if (debit.HasValue)
                amount = -Math.Abs(debit.Value);
            else if (credit.HasValue)
                amount = Math.Abs(credit.Value);
        }
      
        var description = GetFirstValue(row, "description");
        var merchantName =
            GetFirstValue(row, "merchant_name") ??
            GetFirstValue(row, "merchant_name_alt")?? description;

        var referenceNumber =
            GetFirstValue(row, "reference_number") ??
            GetFirstValue(row, "reference_number_alt");

        var currencyCode = GetFirstValue(row, "currency_code");
        var sourceTransactionId = GetFirstValue(row, "source_transaction_id");
        var merchantAccountId = GetFirstValue(row, "merchant_account_id");
        var cardAccountId = GetFirstValue(row, "card_account_id");
        var statementId = GetFirstValue(row, "statement_id");
        var authorizationCode = GetFirstValue(row, "authorization_code");

        if (!transactionDate.HasValue || !amount.HasValue)
            return null;

        if (string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(merchantName))
            return null;

        var normalizedMerchantName = NormalizeMerchantName(merchantName);

        var tx = new CanonicalTransaction
        {
            LoadId = loadId,
            BusinessId = businessId,
            //BusinessKey = businessKey,
            MerchantAccountId = merchantAccountId,
            CardAccountId = cardAccountId,
            StatementId = statementId,
            SourceTransactionId = sourceTransactionId,
            ReferenceNumber = referenceNumber,
            AuthorizationCode = authorizationCode,
            TransactionDate = transactionDate.Value,
            PostedDate = postedDate,
            TransactionDateTime = transactionDate,
            Amount = amount.Value,
            CurrencyCode = currencyCode ?? "USD",
            MerchantName = merchantName,
            NormalizedMerchantName = normalizedMerchantName,
            Description = description,
            SourceFile = sourceFile,
            RawPayloadJson = JsonSerializer.Serialize(row)
        };

        tx.DedupeKey = BuildDedupKey(tx);

        return tx;
    }

    private static string? GetFirstValue(Dictionary<string, string> row, string canonicalField)
    {
        if (!ColumnAliases.TryGetValue(canonicalField, out var aliases))
            return null;

        foreach (var alias in aliases)
        {
            if (row.TryGetValue(alias, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static bool IsEmptyRow(Dictionary<string, string> row)
    {
        return row.Values.All(string.IsNullOrWhiteSpace);
    }

    private static string NormalizeColumnName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var normalized = name.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "_");
        normalized = Regex.Replace(normalized, @"_+", "_").Trim('_');

        return normalized;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        var formats = new[]
        {
            "M/d/yyyy",
            "MM/dd/yyyy",
            "M/d/yy",
            "MM/dd/yy",
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "M-d-yyyy",
            "MM-d-yyyy",
            "M/d/yyyy h:mm:ss tt",
            "MM/dd/yyyy h:mm:ss tt",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ"
        };

        if (DateTime.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var dt))
        {
            return dt;
        }

        if (DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out dt))
        {
            return dt;
        }

        return null;
    }

    private static decimal? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = value.Trim()
            .Replace("$", "")
            .Replace(",", "");

        if (cleaned.StartsWith("(") && cleaned.EndsWith(")"))
            cleaned = "-" + cleaned[1..^1];

        if (decimal.TryParse(
            cleaned,
            NumberStyles.AllowLeadingSign |
            NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out var amount))
        {
            return amount;
        }

        return null;
    }

    private static string? NormalizeMerchantName(string? merchantName)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
            return merchantName;

        var normalized = merchantName.Trim().ToUpperInvariant();
        normalized = Regex.Replace(normalized, @"\s+", " ");
        normalized = Regex.Replace(normalized, @"[^A-Z0-9 ]", "");

        return normalized;
    }

    private static string BuildDedupKey(CanonicalTransaction tx)
    {
        var raw = string.Join("|",
            tx.BusinessId.ToString(), // use raw GUID string for consistency
            tx.BusinessKey,
            tx.MerchantAccountId?.Trim().ToLowerInvariant() ?? "",
            tx.CardAccountId?.Trim().ToLowerInvariant() ?? "",
            tx.StatementId?.Trim().ToLowerInvariant() ?? "",
            tx.SourceTransactionId?.Trim().ToLowerInvariant() ?? "",
            tx.ReferenceNumber?.Trim().ToLowerInvariant() ?? "",
            tx.AuthorizationCode?.Trim().ToLowerInvariant() ?? "",
            tx.TransactionDate.ToString("yyyy-MM-dd"),
            tx.PostedDate?.ToString("yyyy-MM-dd") ?? "",
            tx.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            tx.CurrencyCode?.Trim().ToLowerInvariant() ?? "",
            tx.NormalizedMerchantName?.Trim().ToLowerInvariant() ?? "",
            tx.Description?.Trim().ToLowerInvariant() ?? "",
            tx.SourceRowNumber
        );

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}