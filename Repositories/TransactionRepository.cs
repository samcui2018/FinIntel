using System.Data;
using Microsoft.Data.SqlClient;
using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Utilities;

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
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var dbTransaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await CreateStageTempTableAsync(connection, dbTransaction, cancellationToken);

            var dataTable = BuildStageDataTable(transactions);

            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, dbTransaction))
            {
                bulkCopy.DestinationTableName = "#StageTransactions";
                bulkCopy.BatchSize = 1000;

                foreach (DataColumn col in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
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

        await using var dbTransaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var sql = """
            INSERT INTO dbo.Transactions
            (
                LoadId,
                BusinessId,
                BusinessKey,
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
                CurrencyCode,
                MerchantName,
                NormalizedMerchantName,
                Description,
                IsPossibleDuplicateCharge,
                DuplicateReason
            )
            OUTPUT
                inserted.LoadId,
                inserted.BusinessId,
                inserted.BusinessKey,
                inserted.MerchantAccountId,
                inserted.CardAccountId,
                inserted.StatementId,
                inserted.SourceTransactionId,
                inserted.ReferenceNumber,
                inserted.AuthorizationCode,
                inserted.TransactionDate,
                inserted.PostedDate,
                inserted.TransactionDateTime,
                inserted.Amount,
                inserted.CurrencyCode,
                inserted.MerchantName,
                inserted.NormalizedMerchantName,
                inserted.Description,
                inserted.IsPossibleDuplicateCharge,
                inserted.DuplicateReason
            SELECT
                s.LoadId,
                s.BusinessId,
                s.BusinessKey,
                s.MerchantAccountId,
                s.CardAccountId,
                s.StatementId,
                s.SourceTransactionId,
                s.ReferenceNumber,
                s.AuthorizationCode,
                s.TransactionDate,
                s.PostedDate,
                s.TransactionDateTime,
                s.Amount,
                s.CurrencyCode,
                s.MerchantName,
                s.NormalizedMerchantName,
                s.Description,
                s.IsPossibleDuplicateCharge,
                s.DuplicateReason
            FROM dbo.TransactionsStaged s
            WHERE s.LoadId = @LoadId
              AND s.IsIngestionDuplicate = 0;
            """;

            await using var cmd = new SqlCommand(sql, connection, dbTransaction);
            cmd.Parameters.AddWithValue("@LoadId", loadId);

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
                    CurrencyCode = reader.GetString(13),
                    //MerchantName = reader.GetString(14),
                    MerchantName = reader.IsDBNull(14)? null: reader.GetString(14),
                    NormalizedMerchantName = reader.GetString(15),
                    Description = reader.GetString(16),

                    IsPossibleDuplicateCharge = reader.GetBoolean(17),
                    DuplicateReason = reader.IsDBNull(18) ? null : reader.GetString(18)
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
        var sql = """
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
        cmd.Parameters.AddWithValue("@LoadId", loadId);
        cmd.Parameters.AddWithValue("@BusinessId", businessId);
        // cmd.Parameters.AddWithValue("@BusinessKey", businessKey);
        cmd.Parameters.AddWithValue("@SourceType", sourceType);
        cmd.Parameters.AddWithValue("@SourceName", sourceName);
        cmd.Parameters.AddWithValue("@RowsInFile", rowsInFile);
        cmd.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateUploadStatusAsync(
        Guid loadId,
        string status,
        int? rowsInserted = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var sql = """
        UPDATE dbo.Uploads
        SET
            Status = @Status,
            RowsInserted = @RowsInserted,
            ErrorMessage = @ErrorMessage,
            CreatedAt = SYSUTCDATETIME()
        WHERE LoadId = @LoadId;
        """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@LoadId", loadId);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@RowsInserted", (object?)rowsInserted ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CreateAnalyticsJobAsync(
        Guid loadId,
        string businessId,
        CancellationToken cancellationToken = default)
    {
        var sql = """
        INSERT INTO dbo.AnalyticsJobs
        (
            LoadId,
            BusinessId,
            Status,
            CreatedUtc
        )
        VALUES
        (
            @LoadId,
            @BusinessId,
            'Pending',
            SYSUTCDATETIME()
        );
        """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@LoadId", loadId);
        cmd.Parameters.AddWithValue("@BusinessId", businessId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CreateStageTempTableAsync(
        SqlConnection connection,
        SqlTransaction dbTransaction,
        CancellationToken cancellationToken)
    {
        var sql = """
        CREATE TABLE #StageTransactions
        (
            LoadId uniqueidentifier NOT NULL,

            BusinessId nvarchar(100) NOT NULL,
            BusinessKey int NOT NULL,
            SourceType nvarchar(50) NOT NULL,
            SourceName nvarchar(255) NOT NULL,
            SourceRowNumber int NOT NULL,

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
            CurrencyCode nvarchar(10) NOT NULL,

            MerchantName nvarchar(255) NULL,
            NormalizedMerchantName nvarchar(255) NOT NULL,
            Description nvarchar(500) NOT NULL,

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

        dt.Columns.Add("BusinessId", typeof(string));
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
        dt.Columns.Add("CurrencyCode", typeof(string));

        dt.Columns.Add("MerchantName", typeof(string));
        dt.Columns.Add("NormalizedMerchantName", typeof(string));
        dt.Columns.Add("Description", typeof(string));

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
            row["SourceType"] = tx.SourceType;
            row["SourceName"] = tx.SourceName;
            row["SourceRowNumber"] = tx.SourceRowNumber;

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
            row["CurrencyCode"] = tx.CurrencyCode;

            row["MerchantName"] = tx.MerchantName;
            row["NormalizedMerchantName"] = tx.NormalizedMerchantName;
            row["Description"] = tx.Description;

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
        var sql = """
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

        -- Possible duplicate charges within this upload, scoped by tenant
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

        -- Possible duplicate charges compared to previous staged rows, scoped by tenant
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
    // private static async Task MarkDuplicatesAsync(
    //     SqlConnection connection,
    //     SqlTransaction dbTransaction,
    //     CancellationToken cancellationToken)
    // {
    //     var sql = """
    //     -- Previously ingested exact source record
    //     UPDATE t
    //     SET
    //         t.IsIngestionDuplicate = 1,
    //         t.DuplicateReason = COALESCE(t.DuplicateReason, 'Duplicate source record from a prior load')
    //     FROM #StageTransactions t
    //     WHERE EXISTS
    //     (
    //         SELECT 1
    //         FROM dbo.TransactionsStaged s
    //         WHERE s.IngestionDedupeKey = t.IngestionDedupeKey
    //           AND s.IsIngestionDuplicate = 0
    //     );

    //     -- Duplicate source record within the same upload
    //     ;WITH ranked AS
    //     (
    //         SELECT
    //             *,
    //             ROW_NUMBER() OVER
    //             (
    //                 PARTITION BY IngestionDedupeKey
    //                 ORDER BY SourceRowNumber
    //             ) AS rn
    //         FROM #StageTransactions
    //         WHERE IngestionDedupeKey IS NOT NULL
    //     )
    //     UPDATE ranked
    //     SET
    //         IsIngestionDuplicate = 1,
    //         DuplicateReason = COALESCE(DuplicateReason, 'Duplicate source record within current upload')
    //     WHERE rn > 1;

    //     -- Possible duplicate charges within this upload
    //     ;WITH charge_groups AS
    //     (
    //         SELECT PossibleDuplicateChargeKey
    //         FROM #StageTransactions
    //         WHERE IsIngestionDuplicate = 0
    //           AND PossibleDuplicateChargeKey IS NOT NULL
    //         GROUP BY PossibleDuplicateChargeKey
    //         HAVING COUNT(*) > 1
    //     )
    //     UPDATE t
    //     SET
    //         t.IsPossibleDuplicateCharge = 1,
    //         t.DuplicateReason = CASE
    //             WHEN t.DuplicateReason IS NULL THEN 'Possible duplicate charge'
    //             ELSE t.DuplicateReason + '; Possible duplicate charge'
    //         END
    //     FROM #StageTransactions t
    //     WHERE t.IsIngestionDuplicate = 0
    //       AND EXISTS
    //       (
    //           SELECT 1
    //           FROM charge_groups g
    //           WHERE g.PossibleDuplicateChargeKey = t.PossibleDuplicateChargeKey
    //       );

    //     -- Possible duplicate charges compared to previously staged rows
    //     UPDATE t
    //     SET
    //         t.IsPossibleDuplicateCharge = 1,
    //         t.DuplicateReason = CASE
    //             WHEN t.DuplicateReason IS NULL THEN 'Possible duplicate charge'
    //             ELSE t.DuplicateReason + '; Possible duplicate charge'
    //         END
    //     FROM #StageTransactions t
    //     WHERE t.IsIngestionDuplicate = 0
    //       AND EXISTS
    //       (
    //           SELECT 1
    //           FROM dbo.TransactionsStaged s
    //           WHERE s.PossibleDuplicateChargeKey = t.PossibleDuplicateChargeKey
    //             AND s.BusinessId = t.BusinessId
    //             AND s.IsIngestionDuplicate = 0
    //       );
    //     """;

    //     await using var cmd = new SqlCommand(sql, connection, dbTransaction);
    //     await cmd.ExecuteNonQueryAsync(cancellationToken);
    // }

    private static async Task<int> InsertStageRowsAsync(
        SqlConnection connection,
        SqlTransaction dbTransaction,
        CancellationToken cancellationToken)
    {
        var sql = """
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
            CurrencyCode,
            MerchantName,
            NormalizedMerchantName,
            Description,
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
            CurrencyCode,
            MerchantName,
            NormalizedMerchantName,
            Description,
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
        var sql = """
        SELECT
            SUM(CASE WHEN IsIngestionDuplicate = 1 THEN 1 ELSE 0 END) AS IngestionDuplicates,
            SUM(CASE WHEN IsPossibleDuplicateCharge = 1 AND IsIngestionDuplicate = 0 THEN 1 ELSE 0 END) AS PossibleDuplicateCharges
        FROM dbo.TransactionsStaged
        WHERE LoadId = @LoadId;
        """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@LoadId", loadId);

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