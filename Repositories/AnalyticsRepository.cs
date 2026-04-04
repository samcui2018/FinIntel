using System.Data;
using FinancialIntelligence.Api.Dtos.Analytics;
using Microsoft.Data.SqlClient;

namespace FinancialIntelligence.Api.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly string _connectionString;

    public AnalyticsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Connection string 'FinIntelConnection' was not found.");
    }

    public async Task<IReadOnlyList<UploadHistoryItemResponse>> GetUploadHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // if (userId <= 0)
        //     throw new ArgumentOutOfRangeException(nameof(userId));

        const string sql = """
            SELECT
                u.LoadId,
                b.BusinessName,
                u.SourceName,
                u.RowsInFile,
                u.RowsInserted,
                u.Status,
                u.CreatedAt
            FROM dbo.Uploads u join dbo.Businesses b
                on u.BusinessId = b.BusinessId
            WHERE u.CreatedByUserId = @UserId
            ORDER BY u.CreatedAt DESC;
            """;

        var results = new List<UploadHistoryItemResponse>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = userId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new UploadHistoryItemResponse
            {
                LoadId = reader.GetGuid(reader.GetOrdinal("LoadId")),
                BusinessName = reader.GetString(reader.GetOrdinal("BusinessName")),
                SourceName = reader.GetString(reader.GetOrdinal("SourceName")),
                RowsInFile = reader.GetInt32(reader.GetOrdinal("RowsInFile")),
                RowsInserted = reader.IsDBNull(reader.GetOrdinal("RowsInserted"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("RowsInserted")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }

        return results;
    }

    public async Task<AnalyticsSummaryDto> GetSummaryAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        // if (userId <= 0)
        //     throw new ArgumentOutOfRangeException(nameof(userId));

        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        const string sql = """
            SELECT
                COUNT(*) AS TransactionCount,
                ISNULL(SUM(t.Amount), 0) AS TotalAmount,
                ISNULL(AVG(CAST(t.Amount AS decimal(18,2))), 0) AS AverageAmount,
                ISNULL(SUM(CASE
                    WHEN YEAR(t.TransactionDate) = YEAR(GETDATE())
                     AND MONTH(t.TransactionDate) = MONTH(GETDATE())
                    THEN t.Amount
                    ELSE 0
                END), 0) AS ThisMonthAmount,
                (
                    SELECT TOP 1 t2.MerchantName
                    FROM dbo.Transactions t2
                    INNER JOIN dbo.Uploads u2
                        ON u2.LoadId = t2.LoadId
                    WHERE u2.CreatedByUserId = @UserId
                      AND u2.BusinessId = @BusinessId
                      AND t2.MerchantName IS NOT NULL
                      AND LTRIM(RTRIM(t2.MerchantName)) <> ''
                    GROUP BY t2.MerchantName
                    ORDER BY SUM(t2.Amount) DESC
                ) AS TopMerchant,
                (
                    SELECT MAX(u3.CreatedAt)
                    FROM dbo.Uploads u3
                    WHERE u3.CreatedByUserId = @UserId
                      AND u3.BusinessId = @BusinessId
                ) AS LatestUploadAt
            FROM dbo.Transactions t
            INNER JOIN dbo.Uploads u
                ON u.LoadId = t.LoadId
            WHERE u.CreatedByUserId = @UserId
              AND u.BusinessId = @BusinessId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = userId });
        command.Parameters.Add(new SqlParameter("@BusinessId", SqlDbType.UniqueIdentifier) { Value = businessId });

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AnalyticsSummaryDto();
        }

        return new AnalyticsSummaryDto
        {
            TransactionCount = reader.GetInt32(reader.GetOrdinal("TransactionCount")),
            TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
            AverageAmount = reader.GetDecimal(reader.GetOrdinal("AverageAmount")),
            ThisMonthAmount = reader.GetDecimal(reader.GetOrdinal("ThisMonthAmount")),
            TopMerchant = reader.IsDBNull(reader.GetOrdinal("TopMerchant"))
                ? null
                : reader.GetString(reader.GetOrdinal("TopMerchant")),
            LatestUploadAt = reader.IsDBNull(reader.GetOrdinal("LatestUploadAt"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("LatestUploadAt"))
        };
    }

    public async Task<IReadOnlyList<MonthlyTrendPointResponse>> GetMonthlyTrendAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        // if (userId <= 0)
        //     throw new ArgumentOutOfRangeException(nameof(userId));

        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        const string sql = """
            SELECT
                CONVERT(char(7), t.TransactionDate, 120) AS [Month],
                SUM(t.Amount) AS TotalAmount,
                COUNT(*) AS TransactionCount
            FROM dbo.Transactions t
            INNER JOIN dbo.Uploads u
                ON u.LoadId = t.LoadId
            WHERE u.CreatedByUserId = @UserId
              AND u.BusinessId = @BusinessId
            GROUP BY CONVERT(char(7), t.TransactionDate, 120)
            ORDER BY [Month];
            """;

        var results = new List<MonthlyTrendPointResponse>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = userId });
        command.Parameters.Add(new SqlParameter("@BusinessId", SqlDbType.UniqueIdentifier) { Value = businessId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MonthlyTrendPointResponse
            {
                Month = reader.GetString(reader.GetOrdinal("Month")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                TransactionCount = reader.GetInt32(reader.GetOrdinal("TransactionCount"))
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<TopMerchantResponse>> GetTopMerchantsAsync(
        Guid userId,
        Guid businessId,
        int top,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
             throw new ArgumentOutOfRangeException(nameof(userId));
            //throw new ArgumentOutOfRangeException(nameof(userId));

        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        if (top <= 0)
            throw new ArgumentOutOfRangeException(nameof(top));

        const string sql = """
            SELECT TOP (@Top)
                MerchantName =
                    CASE
                        WHEN t.MerchantName IS NULL OR LTRIM(RTRIM(t.MerchantName)) = ''
                        THEN 'Unknown'
                        ELSE t.MerchantName
                    END,
                SUM(t.Amount) AS TotalAmount,
                COUNT(*) AS TransactionCount
            FROM dbo.Transactions t
            INNER JOIN dbo.Uploads u
                ON u.LoadId = t.LoadId
            WHERE u.CreatedByUserId = @UserId
              AND u.BusinessId = @BusinessId
            GROUP BY
                CASE
                    WHEN t.MerchantName IS NULL OR LTRIM(RTRIM(t.MerchantName)) = ''
                    THEN 'Unknown'
                    ELSE t.MerchantName
                END
            ORDER BY SUM(t.Amount) DESC;
            """;

        var results = new List<TopMerchantResponse>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Top", SqlDbType.Int) { Value = top });
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = userId });
        command.Parameters.Add(new SqlParameter("@BusinessId", SqlDbType.UniqueIdentifier) { Value = businessId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TopMerchantResponse
            {
                MerchantName = reader.GetString(reader.GetOrdinal("MerchantName")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                TransactionCount = reader.GetInt32(reader.GetOrdinal("TransactionCount"))
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<MonthlySpendDto>> GetMonthlySpendAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT
            DATEFROMPARTS(YEAR(TransactionDate), MONTH(TransactionDate), 1) AS MonthStart,
            SUM(Amount) AS Amount
        FROM dbo.Transactions
        WHERE BusinessId = @BusinessId
          AND TransactionDate >= DATEADD(MONTH, -@MonthsBack, CAST(GETUTCDATE() AS date))
        GROUP BY DATEFROMPARTS(YEAR(TransactionDate), MONTH(TransactionDate), 1)
        ORDER BY MonthStart;
        """;

        var results = new List<MonthlySpendDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BusinessId", businessId);
        command.Parameters.AddWithValue("@MonthsBack", monthsBack);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MonthlySpendDto
            {
                MonthStart = reader.GetDateTime(0),
                Amount = reader.GetDecimal(1)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<TopMerchantDto>> GetTopMerchantSpendAsync(
        Guid businessId,
        int monthsBack,
        int topN,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT TOP (@TopN)
            COALESCE(NULLIF(LTRIM(RTRIM(NormalizedMerchantName)), ''), NULLIF(LTRIM(RTRIM(MerchantName)), ''), 'UNKNOWN') AS MerchantName,
            SUM(Amount) AS Amount
        FROM dbo.Transactions
        WHERE BusinessId = @BusinessId
          AND TransactionDate >= DATEADD(MONTH, -@MonthsBack, CAST(GETUTCDATE() AS date))
        GROUP BY COALESCE(NULLIF(LTRIM(RTRIM(NormalizedMerchantName)), ''), NULLIF(LTRIM(RTRIM(MerchantName)), ''), 'UNKNOWN')
        ORDER BY SUM(Amount) DESC;
        """;

        var results = new List<TopMerchantDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BusinessId", businessId);
        command.Parameters.AddWithValue("@MonthsBack", monthsBack);
        command.Parameters.AddWithValue("@TopN", topN);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TopMerchantDto
            {
                MerchantName = reader.GetString(0),
                TotalAmount = reader.GetDecimal(1)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<CategorySpendDto>> GetMonthlyCategorySpendAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT
            DATEFROMPARTS(YEAR(TransactionDate), MONTH(TransactionDate), 1) AS MonthStart,
            CASE
                WHEN NormalizedMerchantName LIKE '%AMAZON%' THEN 'Retail'
                WHEN NormalizedMerchantName LIKE '%UBER%' OR NormalizedMerchantName LIKE '%LYFT%' THEN 'Transportation'
                WHEN NormalizedMerchantName LIKE '%STARBUCKS%' OR NormalizedMerchantName LIKE '%MCDONALD%' THEN 'Meals'
                WHEN Description LIKE '%SUBSCRIPTION%' OR Description LIKE '%MONTHLY%' THEN 'Subscriptions'
                ELSE 'Other'
            END AS Category,
            SUM(Amount) AS Amount
        FROM dbo.Transactions
        WHERE BusinessId = @BusinessId
          AND TransactionDate >= DATEADD(MONTH, -@MonthsBack, CAST(GETUTCDATE() AS date))
        GROUP BY
            DATEFROMPARTS(YEAR(TransactionDate), MONTH(TransactionDate), 1),
            CASE
                WHEN NormalizedMerchantName LIKE '%AMAZON%' THEN 'Retail'
                WHEN NormalizedMerchantName LIKE '%UBER%' OR NormalizedMerchantName LIKE '%LYFT%' THEN 'Transportation'
                WHEN NormalizedMerchantName LIKE '%STARBUCKS%' OR NormalizedMerchantName LIKE '%MCDONALD%' THEN 'Meals'
                WHEN Description LIKE '%SUBSCRIPTION%' OR Description LIKE '%MONTHLY%' THEN 'Subscriptions'
                ELSE 'Other'
            END
        ORDER BY MonthStart, Category;
        """;

        var results = new List<CategorySpendDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BusinessId", businessId);
        command.Parameters.AddWithValue("@MonthsBack", monthsBack);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new CategorySpendDto
            {
                MonthStart = reader.GetDateTime(0),
                Category = reader.GetString(1),
                TotalAmount = reader.GetDecimal(2)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<DuplicateChargeDto>> GetPossibleDuplicateChargesAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT
            COALESCE(NULLIF(LTRIM(RTRIM(NormalizedMerchantName)), ''), NULLIF(LTRIM(RTRIM(MerchantName)), ''), 'UNKNOWN') AS MerchantName,
            CAST(TransactionDate AS date) AS TransactionDate,
            Amount,
            COUNT(*) AS DuplicateCount
        FROM dbo.Transactions
        WHERE BusinessId = @BusinessId
          AND TransactionDate >= DATEADD(MONTH, -@MonthsBack, CAST(GETUTCDATE() AS date))
        GROUP BY
            COALESCE(NULLIF(LTRIM(RTRIM(NormalizedMerchantName)), ''), NULLIF(LTRIM(RTRIM(MerchantName)), ''), 'UNKNOWN'),
            CAST(TransactionDate AS date),
            Amount
        HAVING COUNT(*) > 1
        ORDER BY COUNT(*) DESC, Amount DESC;
        """;

        var results = new List<DuplicateChargeDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BusinessId", businessId);
        command.Parameters.AddWithValue("@MonthsBack", monthsBack);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DuplicateChargeDto
            {
                MerchantName = reader.GetString(0),
                TransactionDate = reader.GetDateTime(1),
                Amount = reader.GetDecimal(2),
                DuplicateCount = reader.GetInt32(3)
            });
        }

        return results;
    }

}