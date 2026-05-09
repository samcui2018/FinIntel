using Dapper;
using Microsoft.Data.SqlClient;
using FinancialIntelligence.Api.Repositories;
using CsvHelper;

namespace FinancialIntelligence.Api.Services;

public sealed class DemoDataSeeder : IDemoDataSeeder
{
    public async Task<Guid> SeedAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid businessId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var loadId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var transactions = BuildDemoTransactions(loadId, businessId, nowUtc);

        const string insertUploadSql = """
            INSERT INTO dbo.Uploads
            (
                LoadId,
                CreatedByUserId,
                BusinessId,
                SourceType,
                SourceName,
                RowsInFile,
                RowsInserted,
                Status,
                ErrorMessage,
                CreatedAt,
                BusinessKey
            )
            VALUES
            (
                @LoadId,
                @CreatedByUserId,
                @BusinessId,
                @SourceType,
                @SourceName,
                @RowsInFile,
                @RowsInserted,
                @Status,
                @ErrorMessage,
                @CreatedAt,
                @BusinessKey
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertUploadSql,
                new
                {
                    LoadId = loadId,
                    CreatedByUserId = createdByUserId,
                    BusinessId = businessId,
                    SourceType = "Demo",
                    SourceName = "Demo Seed Data",
                    RowsInFile = transactions.Count,
                    RowsInserted = transactions.Count,
                    Status = "Completed",
                    ErrorMessage = (string?)null,
                    CreatedAt = nowUtc,
                    BusinessKey = (int?)null
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string insertTransactionSql = """
            INSERT INTO dbo.Transactions
            (
                LoadId,
                BusinessId,
                TransactionDate,
                Description,
                Amount,
                Currency,
                MerchantName,
                CreatedAt,
                IsPossibleDuplicateCharge,
                DuplicateReason,
                PostedDate,
                TransactionDateTime,
                CurrencyCode,
                NormalizedMerchantName,
                MerchantAccountId,
                CardAccountId,
                StatementId,
                SourceTransactionId,
                ReferenceNumber,
                AuthorizationCode,
                BusinessKey,
                TransactionId,
                RawAmount,
                SignedAmount,
                AbsoluteAmount,
                EntryDirection,
                TransactionClass,
                CountsAsSpend,
                CountsAsRevenue,
                CountsAsTransfer,
                CountsAsDebtService,
                RawTransactionType,
                RawDebitCreditIndicator,
                SourceProfileId,
                DirectionConfidence,
                ClassificationConfidence,
                DirectionRuleApplied,
                ClassificationRuleApplied,
                NormalizationNotes,
                Channel
            )
            VALUES
            (
                @LoadId,
                @BusinessId,
                @TransactionDate,
                @Description,
                @Amount,
                @Currency,
                @MerchantName,
                @CreatedAt,
                @IsPossibleDuplicateCharge,
                @DuplicateReason,
                @PostedDate,
                @TransactionDateTime,
                @CurrencyCode,
                @NormalizedMerchantName,
                @MerchantAccountId,
                @CardAccountId,
                @StatementId,
                @SourceTransactionId,
                @ReferenceNumber,
                @AuthorizationCode,
                @BusinessKey,
                @TransactionId,
                @RawAmount,
                @SignedAmount,
                @AbsoluteAmount,
                @EntryDirection,
                @TransactionClass,
                @CountsAsSpend,
                @CountsAsRevenue,
                @CountsAsTransfer,
                @CountsAsDebtService,
                @RawTransactionType,
                @RawDebitCreditIndicator,
                @SourceProfileId,
                @DirectionConfidence,
                @ClassificationConfidence,
                @DirectionRuleApplied,
                @ClassificationRuleApplied,
                @NormalizationNotes,
                @Channel
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertTransactionSql,
                transactions,
                transaction: transaction,
                cancellationToken: cancellationToken));

        return loadId;
    }

    private static List<DemoTransactionRow> BuildDemoTransactions(Guid loadId, Guid businessId, DateTime nowUtc)
    {
        var today = nowUtc.Date;

        var seeds = new List<DemoSeed>
        {
            new(today.AddDays(-90), "Microsoft 365 Business Standard", 89.99m, "Microsoft"),
            new(today.AddDays(-88), "AWS EC2 and storage charges", 412.27m, "Amazon Web Services"),
            new(today.AddDays(-86), "Office supplies order", 146.32m, "Staples"),
            new(today.AddDays(-84), "Shipping charges", 78.44m, "UPS"),
            new(today.AddDays(-82), "Client lunch meeting", 63.18m, "Panera Bread"),
            new(today.AddDays(-80), "Fuel expense", 57.91m, "Shell"),
            new(today.AddDays(-78), "QuickBooks subscription", 95.00m, "Intuit"),
            new(today.AddDays(-76), "Adobe Creative Cloud", 64.99m, "Adobe"),

            new(today.AddDays(-72), "Laptop purchase for analyst", 1849.00m, "Dell"),
            new(today.AddDays(-70), "AWS EC2 and storage charges", 438.55m, "Amazon Web Services"),
            new(today.AddDays(-68), "Team travel airfare", 529.40m, "Delta"),
            new(today.AddDays(-67), "Hotel stay for conference", 688.22m, "Marriott"),
            new(today.AddDays(-65), "Ride share to client site", 34.80m, "Uber"),
            new(today.AddDays(-63), "Office snacks and coffee", 118.09m, "Costco"),
            new(today.AddDays(-61), "Shipping charges", 83.62m, "FedEx"),
            new(today.AddDays(-60), "Fuel expense", 61.10m, "Exxon"),

            new(today.AddDays(-56), "Payment processing software", 249.00m, "Stripe"),
            new(today.AddDays(-54), "Microsoft 365 Business Standard", 89.99m, "Microsoft"),
            new(today.AddDays(-52), "AWS EC2 and storage charges", 455.18m, "Amazon Web Services"),
            new(today.AddDays(-50), "Marketing campaign spend", 920.00m, "LinkedIn"),
            new(today.AddDays(-48), "Office chairs purchase", 1295.00m, "Office Depot"),
            new(today.AddDays(-46), "Client dinner meeting", 142.56m, "The Chop House"),
            new(today.AddDays(-44), "Shipping charges", 76.95m, "UPS"),
            new(today.AddDays(-42), "QuickBooks subscription", 95.00m, "Intuit"),

            new(today.AddDays(-38), "Fuel expense", 58.20m, "Shell"),
            new(today.AddDays(-36), "Office supplies order", 164.91m, "Amazon Business"),
            new(today.AddDays(-34), "Adobe Creative Cloud", 64.99m, "Adobe"),
            new(today.AddDays(-32), "AWS EC2 and storage charges", 472.63m, "Amazon Web Services"),
            new(today.AddDays(-30), "Team lunch", 71.35m, "Chipotle"),
            new(today.AddDays(-28), "Shipping charges", 89.25m, "FedEx"),

            new(today.AddDays(-24), "Software renewal charge", 299.99m, "Zoom", true, "Possible duplicate demo pair"),
            new(today.AddDays(-24), "Software renewal charge", 299.99m, "Zoom", true, "Possible duplicate demo pair"),

            new(today.AddDays(-22), "Fuel expense", 62.84m, "Shell"),
            new(today.AddDays(-20), "QuickBooks subscription", 95.00m, "Intuit"),
            new(today.AddDays(-18), "Microsoft 365 Business Standard", 89.99m, "Microsoft"),
            new(today.AddDays(-16), "Large equipment purchase", 4200.00m, "CDW"),
            new(today.AddDays(-14), "Shipping charges", 92.15m, "UPS"),
            new(today.AddDays(-12), "AWS EC2 and storage charges", 498.30m, "Amazon Web Services"),
            new(today.AddDays(-10), "Office snacks and beverages", 126.48m, "Costco"),
            new(today.AddDays(-8), "Ride share to airport", 41.75m, "Uber"),
            new(today.AddDays(-6), "Hotel stay for client visit", 734.12m, "Hilton"),
            new(today.AddDays(-4), "Client dinner meeting", 156.90m, "Seasons 52"),
            new(today.AddDays(-3), "Fuel expense", 59.73m, "Exxon"),
            new(today.AddDays(-2), "Shipping charges", 81.34m, "FedEx"),
            new(today.AddDays(-1), "AWS EC2 and storage charges", 521.11m, "Amazon Web Services")
        };

        var rows = new List<DemoTransactionRow>(seeds.Count);

        for (var i = 0; i < seeds.Count; i++)
        {
            var seed = seeds[i];
            var txDateTime = seed.TransactionDate.AddHours(10).AddMinutes(i % 50);
            var normalizedMerchant = NormalizeMerchantName(seed.MerchantName);
            var referenceSuffix = (100000 + i).ToString();
            var authSuffix = (700000 + i).ToString();

            rows.Add(new DemoTransactionRow
            {
                LoadId = loadId,
                BusinessId = businessId,
                TransactionDate = Convert.ToDateTime(seed.TransactionDate),
                Description = seed.Description,
                Amount = seed.Amount,
                Currency = "USD",
                MerchantName = seed.MerchantName,
                CreatedAt = nowUtc,
                IsPossibleDuplicateCharge = seed.IsPossibleDuplicateCharge,
                DuplicateReason = seed.DuplicateReason,
                PostedDate = txDateTime.AddHours(6),
                TransactionDateTime = txDateTime,
                CurrencyCode = "USD",
                NormalizedMerchantName = normalizedMerchant,
                MerchantAccountId = "DEMO-MERCHANT-001",
                CardAccountId = "DEMO-CARD-001",
                StatementId = $"DEMO-STMT-{seed.TransactionDate:yyyyMM}",
                SourceTransactionId = $"DEMO-TXN-{i + 1:0000}",
                ReferenceNumber = $"REF-{referenceSuffix}",
                AuthorizationCode = $"AUTH-{authSuffix}",
                BusinessKey = null,
                TransactionId = Guid.NewGuid(),
                RawAmount = seed.Amount,
                SignedAmount = seed.Amount,
                AbsoluteAmount = Math.Abs(seed.Amount),
                EntryDirection = 1,
                TransactionClass = 1,
                CountsAsSpend = true,
                CountsAsRevenue = false,
                CountsAsTransfer = false,
                CountsAsDebtService = false,
                RawTransactionType = "CardPurchase",
                RawDebitCreditIndicator = "Debit",
                SourceProfileId = "DEMO-PROFILE-001",
                DirectionConfidence = 5,
                ClassificationConfidence = 5,
                DirectionRuleApplied = "DemoSeedDebitSpend",
                ClassificationRuleApplied = "DemoSeedOperatingExpense",
                NormalizationNotes = seed.IsPossibleDuplicateCharge
                    ? "Seeded demo duplicate example."
                    : "Seeded demo transaction.",
                Channel = InferChannel(seed.MerchantName, seed.Description)
            });
        }

        return rows;
    }

    private static string NormalizeMerchantName(string merchantName)
    {
        return merchantName.Trim().ToUpperInvariant();
    }

    private static string InferChannel(string merchantName, string description)
    {
        var value = $"{merchantName} {description}".ToUpperInvariant();

        if (value.Contains("UBER") || value.Contains("DELTA") || value.Contains("MARRIOTT") || value.Contains("HILTON"))
            return "Travel";

        if (value.Contains("AWS") || value.Contains("MICROSOFT") || value.Contains("ADOBE") || value.Contains("ZOOM") || value.Contains("INTUIT") || value.Contains("STRIPE"))
            return "Online";

        if (value.Contains("UPS") || value.Contains("FEDEX"))
            return "Shipping";

        if (value.Contains("SHELL") || value.Contains("EXXON"))
            return "Fuel";

        return "Card";
    }

    private sealed record DemoSeed(
        DateTime TransactionDate,
        string Description,
        decimal Amount,
        string MerchantName,
        bool IsPossibleDuplicateCharge = false,
        string? DuplicateReason = null);

    private sealed class DemoTransactionRow
    {
        public Guid LoadId { get; init; }
        public Guid? BusinessId { get; init; }
        public DateTime TransactionDate { get; init; }
        public string? Description { get; init; }
        public decimal Amount { get; init; }
        public string? Currency { get; init; }
        public string? MerchantName { get; init; }
        public DateTime CreatedAt { get; init; }
        public bool IsPossibleDuplicateCharge { get; init; }
        public string? DuplicateReason { get; init; }
        public DateTime? PostedDate { get; init; }
        public DateTime? TransactionDateTime { get; init; }
        public string CurrencyCode { get; init; } = "USD";
        public string? NormalizedMerchantName { get; init; }
        public string? MerchantAccountId { get; init; }
        public string? CardAccountId { get; init; }
        public string? StatementId { get; init; }
        public string? SourceTransactionId { get; init; }
        public string? ReferenceNumber { get; init; }
        public string? AuthorizationCode { get; init; }
        public int? BusinessKey { get; init; }
        public Guid TransactionId { get; init; }
        public decimal RawAmount { get; init; }
        public decimal SignedAmount { get; init; }
        public decimal AbsoluteAmount { get; init; }
        public byte EntryDirection { get; init; }
        public byte TransactionClass { get; init; }
        public bool CountsAsSpend { get; init; }
        public bool CountsAsRevenue { get; init; }
        public bool CountsAsTransfer { get; init; }
        public bool CountsAsDebtService { get; init; }
        public string? RawTransactionType { get; init; }
        public string? RawDebitCreditIndicator { get; init; }
        public string? SourceProfileId { get; init; }
        public byte DirectionConfidence { get; init; }
        public byte ClassificationConfidence { get; init; }
        public string? DirectionRuleApplied { get; init; }
        public string? ClassificationRuleApplied { get; init; }
        public string? NormalizationNotes { get; init; }
        public string? Channel { get; init; }
    }
}