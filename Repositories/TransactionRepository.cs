
using System.Data;
using Microsoft.Data.SqlClient;
using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Utilities;
using FinancialIntelligence.Api.Domain.Transactions;

namespace FinancialIntelligence.Api.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly string _connectionString;

    public TransactionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("FinIntelConnection is missing.");
    }

    public async Task<int> StageTransactionsAsync(
        IReadOnlyList<CanonicalTransaction> transactions,
        CancellationToken cancellationToken = default)
    {
        if (transactions.Count == 0)
            return 0;

        foreach (var tx in transactions)
        {
            TransactionKeyBuilder.Enrich(tx);

            // Safety defaults so staging remains consistent even if upstream normalization is partial.
            if (tx.RawAmount == 0m && tx.Amount != 0m)
            {
                tx.RawAmount = tx.Amount;
            }

            if (tx.AbsoluteAmount == 0m && (tx.SignedAmount != 0m || tx.RawAmount != 0m))
            {
                tx.AbsoluteAmount = Math.Abs(tx.SignedAmount != 0m ? tx.SignedAmount : tx.RawAmount);
            }

            if (tx.SignedAmount == 0m && tx.RawAmount != 0m)
            {
                tx.SignedAmount = tx.RawAmount;
            }

            // Keep backward-compatible Amount aligned to signed amount.
            tx.Amount = tx.SignedAmount;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var dbTransaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await CreateStageTempTableAsync(connection, dbTransaction, cancellationToken);

            var dataTable = BuildStageDataTable(transactions);

            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, dbTransaction))
            {
                bulkCopy.DestinationTableName = "#StageTransactions";
                bulkCopy.BatchSize = 1000;

                foreach (DataColumn column in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
            }

            await MarkDuplicatesAsync(connection, dbTransaction, cancellationToken);
            var insertedCount = await InsertStageRowsAsync(connection, dbTransaction, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);
            return insertedCount;
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(int InsertedCount, List<CanonicalTransaction> InsertedRows)> PromoteTransactionsAsync(
        Guid loadId,
        CancellationToken cancellationToken = default)
    {
        var insertedRows = new List<CanonicalTransaction>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var dbTransaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var cmd = new SqlCommand("dbo.spPromoteTransactions", connection, dbTransaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@LoadId", SqlDbType.UniqueIdentifier).Value = loadId;

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                insertedRows.Add(new CanonicalTransaction
                {
                    LoadId = reader.GetGuid(0),
                    BusinessId = reader.GetGuid(1),
                    BusinessKey = reader.GetInt32(2),

                    MerchantAccountId = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CardAccountId = reader.IsDBNull(4) ? null : reader.GetString(4),
                    StatementId = reader.IsDBNull(5) ? null : reader.GetString(5),
                    SourceTransactionId = reader.IsDBNull(6) ? null : reader.GetString(6),
                    ReferenceNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                    AuthorizationCode = reader.IsDBNull(8) ? null : reader.GetString(8),

                    TransactionDate = reader.GetDateTime(9),
                    PostedDate = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    TransactionDateTime = reader.IsDBNull(11) ? null : reader.GetDateTime(11),

                    Amount = reader.GetDecimal(12),
                    RawAmount = reader.GetDecimal(13),
                    SignedAmount = reader.GetDecimal(14),
                    AbsoluteAmount = reader.GetDecimal(15),

                    CurrencyCode = reader.GetString(16),
                    MerchantName = reader.IsDBNull(17) ? null : reader.GetString(17),
                    NormalizedMerchantName = reader.IsDBNull(18) ? null : reader.GetString(18),
                    Description = reader.IsDBNull(19) ? null : reader.GetString(19),
                    Channel = reader.IsDBNull(20) ? null : reader.GetString(20),

                    EntryDirection = (EntryDirection)reader.GetByte(21),
                    TransactionClass = (TransactionClass)reader.GetByte(22),
                    CountsAsSpend = reader.GetBoolean(23),
                    CountsAsRevenue = reader.GetBoolean(24),
                    CountsAsTransfer = reader.GetBoolean(25),
                    CountsAsDebtService = reader.GetBoolean(26),

                    RawTransactionType = reader.IsDBNull(27) ? null : reader.GetString(27),
                    RawDebitCreditIndicator = reader.IsDBNull(28) ? null : reader.GetString(28),
                    SourceProfileId = reader.IsDBNull(29) ? null : reader.GetString(29),

                    DirectionConfidence = (ConfidenceLevel)reader.GetByte(30),
                    ClassificationConfidence = (ConfidenceLevel)reader.GetByte(31),
                    DirectionRuleApplied = reader.IsDBNull(32) ? null : reader.GetString(32),
                    ClassificationRuleApplied = reader.IsDBNull(33) ? null : reader.GetString(33),
                    NormalizationNotes = reader.IsDBNull(34) ? null : reader.GetString(34),

                    IsPossibleDuplicateCharge = reader.GetBoolean(35),
                    DuplicateReason = reader.IsDBNull(36) ? null : reader.GetString(36)
                });
            }

            await reader.CloseAsync();
            await dbTransaction.CommitAsync(cancellationToken);

            return (insertedRows.Count, insertedRows);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task CreateUploadRecordAsync(
        Guid loadId,
        Guid businessId,
        string sourceType,
        string sourceName,
        int rowsInFile,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Uploads
            (
                LoadId,
                BusinessId,
                SourceType,
                SourceName,
                RowsInFile,
                CreatedByUserId,
                Status,
                CreatedAt
            )
            VALUES
            (
                @LoadId,
                @BusinessId,
                @SourceType,
                @SourceName,
                @RowsInFile,
                @CreatedByUserId,
                'Received',
                SYSUTCDATETIME()
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@LoadId", SqlDbType.UniqueIdentifier).Value = loadId;
        cmd.Parameters.Add("@BusinessId", SqlDbType.UniqueIdentifier).Value = businessId;
        cmd.Parameters.Add("@SourceType", SqlDbType.NVarChar, 50).Value = sourceType;
        cmd.Parameters.Add("@SourceName", SqlDbType.NVarChar, 255).Value = sourceName;
        cmd.Parameters.Add("@RowsInFile", SqlDbType.Int).Value = rowsInFile;
        cmd.Parameters.Add("@CreatedByUserId", SqlDbType.UniqueIdentifier).Value = createdByUserId;

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateUploadStatusAsync(
        Guid loadId,
        string status,
        int? rowsInserted = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Uploads
            SET
                Status = @Status,
                RowsInserted = @RowsInserted,
                ErrorMessage = @ErrorMessage
            WHERE LoadId = @LoadId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@LoadId", SqlDbType.UniqueIdentifier).Value = loadId;
        cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = status;
        cmd.Parameters.Add("@RowsInserted", SqlDbType.Int).Value = (object?)rowsInserted ?? DBNull.Value;
        cmd.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, -1).Value = (object?)errorMessage ?? DBNull.Value;

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CreateStageTempTableAsync(
        SqlConnection connection,
        SqlTransaction dbTransaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE #StageTransactions
            (
                LoadId uniqueidentifier NOT NULL,

                BusinessId uniqueidentifier NOT NULL,
                BusinessKey int NOT NULL,
                SourceType nvarchar(50) NULL,
                SourceName nvarchar(255) NULL,
                SourceRowNumber int NULL,

                MerchantAccountId nvarchar(100) NULL,
                CardAccountId nvarchar(100) NULL,
                StatementId nvarchar(100) NULL,
                SourceTransactionId nvarchar(100) NULL,
                ReferenceNumber nvarchar(100) NULL,
                AuthorizationCode nvarchar(100) NULL,

                TransactionDate datetime2(0) NOT NULL,
                PostedDate datetime2(0) NULL,
                TransactionDateTime datetime2(0) NULL,

                Amount decimal(18,2) NOT NULL,
                RawAmount decimal(18,2) NOT NULL,
                SignedAmount decimal(18,2) NOT NULL,
                AbsoluteAmount decimal(18,2) NOT NULL,
                CurrencyCode nvarchar(10) NOT NULL,

                MerchantName nvarchar(255) NULL,
                NormalizedMerchantName nvarchar(255) NULL,
                Description nvarchar(500) NULL,

                EntryDirection tinyint NOT NULL,
                TransactionClass tinyint NOT NULL,
                CountsAsSpend bit NOT NULL,
                CountsAsRevenue bit NOT NULL,
                CountsAsTransfer bit NOT NULL,
                CountsAsDebtService bit NOT NULL,

                RawTransactionType nvarchar(100) NULL,
                RawDebitCreditIndicator nvarchar(50) NULL,
                SourceProfileId nvarchar(100) NULL,

                DirectionConfidence tinyint NOT NULL,
                ClassificationConfidence tinyint NOT NULL,
                DirectionRuleApplied nvarchar(100) NULL,
                ClassificationRuleApplied nvarchar(100) NULL,
                NormalizationNotes nvarchar(500) NULL,

                IngestionDedupeKey varbinary(32) NULL,
                IsIngestionDuplicate bit NOT NULL,

                PossibleDuplicateChargeKey varbinary(32) NULL,
                IsPossibleDuplicateCharge bit NOT NULL,

                DuplicateReason nvarchar(300) NULL
            );
            """;

        await using var cmd = new SqlCommand(sql, connection, dbTransaction);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DataTable BuildStageDataTable(IReadOnlyList<CanonicalTransaction> transactions)
    {
        var dt = new DataTable();

        dt.Columns.Add("LoadId", typeof(Guid));

        dt.Columns.Add("BusinessId", typeof(Guid));
        dt.Columns.Add("BusinessKey", typeof(int));
        dt.Columns.Add("SourceType", typeof(string));
        dt.Columns.Add("SourceName", typeof(string));
        dt.Columns.Add("SourceRowNumber", typeof(int));

        dt.Columns.Add("MerchantAccountId", typeof(string));
        dt.Columns.Add("CardAccountId", typeof(string));
        dt.Columns.Add("StatementId", typeof(string));
        dt.Columns.Add("SourceTransactionId", typeof(string));
        dt.Columns.Add("ReferenceNumber", typeof(string));
        dt.Columns.Add("AuthorizationCode", typeof(string));

        dt.Columns.Add("TransactionDate", typeof(DateTime));
        dt.Columns.Add("PostedDate", typeof(DateTime));
        dt.Columns.Add("TransactionDateTime", typeof(DateTime));

        dt.Columns.Add("Amount", typeof(decimal));
        dt.Columns.Add("RawAmount", typeof(decimal));
        dt.Columns.Add("SignedAmount", typeof(decimal));
        dt.Columns.Add("AbsoluteAmount", typeof(decimal));
        dt.Columns.Add("CurrencyCode", typeof(string));

        dt.Columns.Add("MerchantName", typeof(string));
        dt.Columns.Add("NormalizedMerchantName", typeof(string));
        dt.Columns.Add("Description", typeof(string));

        dt.Columns.Add("EntryDirection", typeof(byte));
        dt.Columns.Add("TransactionClass", typeof(byte));
        dt.Columns.Add("CountsAsSpend", typeof(bool));
        dt.Columns.Add("CountsAsRevenue", typeof(bool));
        dt.Columns.Add("CountsAsTransfer", typeof(bool));
        dt.Columns.Add("CountsAsDebtService", typeof(bool));

        dt.Columns.Add("RawTransactionType", typeof(string));
        dt.Columns.Add("RawDebitCreditIndicator", typeof(string));
        dt.Columns.Add("SourceProfileId", typeof(string));

        dt.Columns.Add("DirectionConfidence", typeof(byte));
        dt.Columns.Add("ClassificationConfidence", typeof(byte));
        dt.Columns.Add("DirectionRuleApplied", typeof(string));
        dt.Columns.Add("ClassificationRuleApplied", typeof(string));
        dt.Columns.Add("NormalizationNotes", typeof(string));

        dt.Columns.Add("IngestionDedupeKey", typeof(byte[]));
        dt.Columns.Add("IsIngestionDuplicate", typeof(bool));

        dt.Columns.Add("PossibleDuplicateChargeKey", typeof(byte[]));
        dt.Columns.Add("IsPossibleDuplicateCharge", typeof(bool));

        dt.Columns.Add("DuplicateReason", typeof(string));

        foreach (var tx in transactions)
        {
            var row = dt.NewRow();

            row["LoadId"] = tx.LoadId;

            row["BusinessId"] = tx.BusinessId;
            row["BusinessKey"] = tx.BusinessKey;
            row["SourceType"] = (object?)tx.SourceType ?? DBNull.Value;
            row["SourceName"] = (object?)tx.SourceName ?? DBNull.Value;
            row["SourceRowNumber"] = (object?)tx.SourceRowNumber ?? DBNull.Value;

            row["MerchantAccountId"] = (object?)tx.MerchantAccountId ?? DBNull.Value;
            row["CardAccountId"] = (object?)tx.CardAccountId ?? DBNull.Value;
            row["StatementId"] = (object?)tx.StatementId ?? DBNull.Value;
            row["SourceTransactionId"] = (object?)tx.SourceTransactionId ?? DBNull.Value;
            row["ReferenceNumber"] = (object?)tx.ReferenceNumber ?? DBNull.Value;
            row["AuthorizationCode"] = (object?)tx.AuthorizationCode ?? DBNull.Value;

            row["TransactionDate"] = tx.TransactionDate;
            row["PostedDate"] = (object?)tx.PostedDate ?? DBNull.Value;
            row["TransactionDateTime"] = (object?)tx.TransactionDateTime ?? DBNull.Value;

            row["Amount"] = tx.Amount;
            row["RawAmount"] = tx.RawAmount;
            row["SignedAmount"] = tx.SignedAmount;
            row["AbsoluteAmount"] = tx.AbsoluteAmount;
            row["CurrencyCode"] = tx.CurrencyCode;

            row["MerchantName"] = (object?)tx.MerchantName ?? DBNull.Value;
            row["NormalizedMerchantName"] = (object?)tx.NormalizedMerchantName ?? DBNull.Value;
            row["Description"] = (object?)tx.Description ?? DBNull.Value;

            row["EntryDirection"] = (byte)tx.EntryDirection;
            row["TransactionClass"] = (byte)tx.TransactionClass;
            row["CountsAsSpend"] = tx.CountsAsSpend;
            row["CountsAsRevenue"] = tx.CountsAsRevenue;
            row["CountsAsTransfer"] = tx.CountsAsTransfer;
            row["CountsAsDebtService"] = tx.CountsAsDebtService;

            row["RawTransactionType"] = (object?)tx.RawTransactionType ?? DBNull.Value;
            row["RawDebitCreditIndicator"] = (object?)tx.RawDebitCreditIndicator ?? DBNull.Value;
            row["SourceProfileId"] = (object?)tx.SourceProfileId ?? DBNull.Value;

            row["DirectionConfidence"] = (byte)tx.DirectionConfidence;
            row["ClassificationConfidence"] = (byte)tx.ClassificationConfidence;
            row["DirectionRuleApplied"] = (object?)tx.DirectionRuleApplied ?? DBNull.Value;
            row["ClassificationRuleApplied"] = (object?)tx.ClassificationRuleApplied ?? DBNull.Value;
            row["NormalizationNotes"] = (object?)tx.NormalizationNotes ?? DBNull.Value;

            row["IngestionDedupeKey"] = (object?)tx.IngestionDedupeKey ?? DBNull.Value;
            row["IsIngestionDuplicate"] = tx.IsIngestionDuplicate;

            row["PossibleDuplicateChargeKey"] = (object?)tx.PossibleDuplicateChargeKey ?? DBNull.Value;
            row["IsPossibleDuplicateCharge"] = tx.IsPossibleDuplicateCharge;

            row["DuplicateReason"] = (object?)tx.DuplicateReason ?? DBNull.Value;

            dt.Rows.Add(row);
        }

        return dt;
    }

    private static async Task MarkDuplicatesAsync(
        SqlConnection connection,
        SqlTransaction dbTransaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            -- Previously ingested exact source record, scoped by tenant
            UPDATE t
            SET
                t.IsIngestionDuplicate = 1,
                t.DuplicateReason = COALESCE(t.DuplicateReason, 'Duplicate source record from a prior load')
            FROM #StageTransactions t
            WHERE EXISTS
            (
                SELECT 1
                FROM dbo.TransactionsStaged s
                WHERE s.BusinessKey = t.BusinessKey
                  AND s.IngestionDedupeKey = t.IngestionDedupeKey
                  AND s.IsIngestionDuplicate = 0
            );

            -- Duplicate within current upload, scoped by tenant
            ;WITH ranked AS
            (
                SELECT
                    *,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY BusinessKey, IngestionDedupeKey
                        ORDER BY SourceRowNumber
                    ) AS rn
                FROM #StageTransactions
                WHERE IngestionDedupeKey IS NOT NULL
            )
            UPDATE ranked
            SET
                IsIngestionDuplicate = 1,
                DuplicateReason = COALESCE(DuplicateReason, 'Duplicate source record within current upload')
            WHERE rn > 1;

            -- Possible duplicate charges within current upload, scoped by tenant
            ;WITH charge_groups AS
            (
                SELECT BusinessKey, PossibleDuplicateChargeKey
                FROM #StageTransactions
                WHERE IsIngestionDuplicate = 0
                  AND PossibleDuplicateChargeKey IS NOT NULL
                GROUP BY BusinessKey, PossibleDuplicateChargeKey
                HAVING COUNT(*) > 1
            )
            UPDATE t
            SET
                t.IsPossibleDuplicateCharge = 1,
                t.DuplicateReason = CASE
                    WHEN t.DuplicateReason IS NULL THEN 'Possible duplicate charge'
                    ELSE t.DuplicateReason + '; Possible duplicate charge'
                END
            FROM #StageTransactions t
            WHERE t.IsIngestionDuplicate = 0
              AND EXISTS
              (
                  SELECT 1
                  FROM charge_groups g
                  WHERE g.BusinessKey = t.BusinessKey
                    AND g.PossibleDuplicateChargeKey = t.PossibleDuplicateChargeKey
              );

            -- Possible duplicate charges compared to previously staged rows, scoped by tenant
            UPDATE t
            SET
                t.IsPossibleDuplicateCharge = 1,
                t.DuplicateReason = CASE
                    WHEN t.DuplicateReason IS NULL THEN 'Possible duplicate charge'
                    ELSE t.DuplicateReason + '; Possible duplicate charge'
                END
            FROM #StageTransactions t
            WHERE t.IsIngestionDuplicate = 0
              AND EXISTS
              (
                  SELECT 1
                  FROM dbo.TransactionsStaged s
                  WHERE s.BusinessKey = t.BusinessKey
                    AND s.PossibleDuplicateChargeKey = t.PossibleDuplicateChargeKey
                    AND s.IsIngestionDuplicate = 0
              );
            """;

        await using var cmd = new SqlCommand(sql, connection, dbTransaction);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> InsertStageRowsAsync(
        SqlConnection connection,
        SqlTransaction dbTransaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.TransactionsStaged
            (
                LoadId,
                BusinessId,
                BusinessKey,
                SourceType,
                SourceName,
                SourceRowNumber,
                MerchantAccountId,
                CardAccountId,
                StatementId,
                SourceTransactionId,
                ReferenceNumber,
                AuthorizationCode,
                TransactionDate,
                PostedDate,
                TransactionDateTime,
                Amount,
                RawAmount,
                SignedAmount,
                AbsoluteAmount,
                CurrencyCode,
                MerchantName,
                NormalizedMerchantName,
                Description,
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
                IngestionDedupeKey,
                IsIngestionDuplicate,
                PossibleDuplicateChargeKey,
                IsPossibleDuplicateCharge,
                DuplicateReason
            )
            SELECT
                LoadId,
                BusinessId,
                BusinessKey,
                SourceType,
                SourceName,
                SourceRowNumber,
                MerchantAccountId,
                CardAccountId,
                StatementId,
                SourceTransactionId,
                ReferenceNumber,
                AuthorizationCode,
                TransactionDate,
                PostedDate,
                TransactionDateTime,
                Amount,
                RawAmount,
                SignedAmount,
                AbsoluteAmount,
                CurrencyCode,
                MerchantName,
                NormalizedMerchantName,
                Description,
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
                IngestionDedupeKey,
                IsIngestionDuplicate,
                PossibleDuplicateChargeKey,
                IsPossibleDuplicateCharge,
                DuplicateReason
            FROM #StageTransactions;
            """;

        await using var cmd = new SqlCommand(sql, connection, dbTransaction);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<(int IngestionDuplicates, int PossibleDuplicateCharges)> GetLoadDuplicateSummaryAsync(
        Guid loadId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                SUM(CASE WHEN IsIngestionDuplicate = 1 THEN 1 ELSE 0 END) AS IngestionDuplicates,
                SUM(CASE WHEN IsPossibleDuplicateCharge = 1 AND IsIngestionDuplicate = 0 THEN 1 ELSE 0 END) AS PossibleDuplicateCharges
            FROM dbo.TransactionsStaged
            WHERE LoadId = @LoadId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@LoadId", SqlDbType.UniqueIdentifier).Value = loadId;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            var ingestionDuplicates = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var possibleDuplicateCharges = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);

            return (ingestionDuplicates, possibleDuplicateCharges);
        }

        return (0, 0);
    }
}