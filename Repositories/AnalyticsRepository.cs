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
        const string sql = """
            SELECT
                u.LoadId,
                b.BusinessName,
                u.SourceName,
                u.RowsInFile,
                u.RowsInserted,
                u.Status,
                u.CreatedAt
            FROM dbo.Uploads u
            INNER JOIN dbo.Businesses b
                ON u.BusinessId = b.BusinessId
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

    public async Task<AnalyticsSummaryDto> GetSpendSummaryAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(userId));

        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        const string sql = """
            SELECT
                COUNT(*) AS TransactionCount,
                ISNULL(SUM(CASE WHEN t.CountsAsSpend = 1 THEN t.AbsoluteAmount ELSE 0 END), 0) AS TotalAmount,
                ISNULL(AVG(CAST(CASE WHEN t.CountsAsSpend = 1 THEN t.AbsoluteAmount END AS decimal(18,2))), 0) AS AverageAmount,
                ISNULL(SUM(CASE
                    WHEN t.CountsAsSpend = 1
                     AND YEAR(t.TransactionDate) = YEAR(GETDATE())
                     AND MONTH(t.TransactionDate) = MONTH(GETDATE())
                    THEN t.AbsoluteAmount
                    ELSE 0
                END), 0) AS ThisMonthAmount,
                (
                    SELECT TOP 1
                        COALESCE(
                            NULLIF(LTRIM(RTRIM(t2.NormalizedMerchantName)), ''),
                            NULLIF(LTRIM(RTRIM(t2.MerchantName)), ''),
                            'Unknown'
                        ) AS MerchantName
                    FROM dbo.Transactions t2
                    INNER JOIN dbo.Uploads u2
                        ON u2.LoadId = t2.LoadId
                    WHERE u2.CreatedByUserId = @UserId
                      AND u2.BusinessId = @BusinessId
                      AND t2.CountsAsSpend = 1
                    GROUP BY COALESCE(
                        NULLIF(LTRIM(RTRIM(t2.NormalizedMerchantName)), ''),
                        NULLIF(LTRIM(RTRIM(t2.MerchantName)), ''),
                        'Unknown'
                    )
                    ORDER BY SUM(t2.AbsoluteAmount) DESC
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

    public async Task<IReadOnlyList<MonthlyTrendPointResponse>> GetMonthlySpendTrendAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(userId));

        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        const string sql = """
            SELECT
                CONVERT(char(7), t.TransactionDate, 120) AS [Month],
                SUM(t.AbsoluteAmount) AS TotalAmount,
                COUNT(*) AS TransactionCount
            FROM dbo.Transactions t
            INNER JOIN dbo.Uploads u
                ON u.LoadId = t.LoadId
            WHERE u.CreatedByUserId = @UserId
              AND u.BusinessId = @BusinessId
              AND t.CountsAsSpend = 1
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

    public async Task<IReadOnlyList<TopMerchantResponse>> GetTopSpendMerchantsAsync(
        Guid userId,
        Guid businessId,
        int top,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(userId));

        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        if (top <= 0)
            throw new ArgumentOutOfRangeException(nameof(top));

        const string sql = """
            SELECT TOP (@Top)
                MerchantName = COALESCE(
                    NULLIF(LTRIM(RTRIM(t.NormalizedMerchantName)), ''),
                    NULLIF(LTRIM(RTRIM(t.MerchantName)), ''),
                    'Unknown'
                ),
                SUM(t.AbsoluteAmount) AS TotalAmount,
                COUNT(*) AS TransactionCount
            FROM dbo.Transactions t
            INNER JOIN dbo.Uploads u
                ON u.LoadId = t.LoadId
            WHERE u.CreatedByUserId = @UserId
              AND u.BusinessId = @BusinessId
              AND t.CountsAsSpend = 1
            GROUP BY COALESCE(
                NULLIF(LTRIM(RTRIM(t.NormalizedMerchantName)), ''),
                NULLIF(LTRIM(RTRIM(t.MerchantName)), ''),
                'Unknown'
            )
            ORDER BY SUM(t.AbsoluteAmount) DESC;
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
        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        if (monthsBack <= 0)
            throw new ArgumentOutOfRangeException(nameof(monthsBack));

        const string sql = """
            SELECT
                DATEFROMPARTS(YEAR(TransactionDate), MONTH(TransactionDate), 1) AS MonthStart,
                SUM(AbsoluteAmount) AS Amount,
                COUNT(*) AS TransactionCount
            FROM dbo.Transactions
            WHERE BusinessId = @BusinessId
              AND CountsAsSpend = 1
             -- AND TransactionDate >= DATEADD(MONTH, -@MonthsBack, CAST(GETUTCDATE() AS date))
            GROUP BY DATEFROMPARTS(YEAR(TransactionDate), MONTH(TransactionDate), 1)
            ORDER BY MonthStart;
            """;

        var results = new List<MonthlySpendDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@BusinessId", SqlDbType.UniqueIdentifier) { Value = businessId });
        command.Parameters.Add(new SqlParameter("@MonthsBack", SqlDbType.Int) { Value = monthsBack });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MonthlySpendDto
            {
                MonthStart = reader.GetDateTime(reader.GetOrdinal("MonthStart")),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount"))
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
        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        if (monthsBack <= 0)
            throw new ArgumentOutOfRangeException(nameof(monthsBack));

        if (topN <= 0)
            throw new ArgumentOutOfRangeException(nameof(topN));

        const string sql = """
            SELECT TOP (@TopN)
                COALESCE(
                    NULLIF(LTRIM(RTRIM(NormalizedMerchantName)), ''),
                    NULLIF(LTRIM(RTRIM(MerchantName)), ''),
                    'UNKNOWN'
                ) AS MerchantName,
                SUM(AbsoluteAmount) AS Amount
            FROM dbo.Transactions
            WHERE BusinessId = @BusinessId
              AND CountsAsSpend = 1
              AND TransactionDate >= DATEADD(MONTH, -@MonthsBack, CAST(GETUTCDATE() AS date))
            GROUP BY COALESCE(
                NULLIF(LTRIM(RTRIM(NormalizedMerchantName)), ''),
                NULLIF(LTRIM(RTRIM(MerchantName)), ''),
                'UNKNOWN'
            )
            ORDER BY SUM(AbsoluteAmount) DESC;
            """;

        var results = new List<TopMerchantDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@BusinessId", SqlDbType.UniqueIdentifier) { Value = businessId });
        command.Parameters.Add(new SqlParameter("@MonthsBack", SqlDbType.Int) { Value = monthsBack });
        command.Parameters.Add(new SqlParameter("@TopN", SqlDbType.Int) { Value = topN });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TopMerchantDto
            {
                MerchantName = reader.GetString(reader.GetOrdinal("MerchantName")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("Amount"))
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<CategorySpendDto>> GetMonthlyCategorySpendAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        if (monthsBack <= 0)
            throw new ArgumentOutOfRangeException(nameof(monthsBack));

        const string sql = """
            SELECT
                DATEFROMPARTS(YEAR(TransactionDate), MONTH(TransactionDate), 1) AS MonthStart,
                CASE
                    WHEN UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%AMAZON%' THEN 'Retail'
                    WHEN UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%UBER%' OR UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%LYFT%' THEN 'Transportation'
                    WHEN UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%STARBUCKS%' OR UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%MCDONALD%' THEN 'Meals'
                    WHEN UPPER(ISNULL(Description, '')) LIKE '%SUBSCRIPTION%' OR UPPER(ISNULL(Description, '')) LIKE '%MONTHLY%' THEN 'Subscriptions'
                    ELSE 'Other'
                END AS Category,
                SUM(AbsoluteAmount) AS Amount
            FROM dbo.Transactions
            WHERE BusinessId = @BusinessId
              AND CountsAsSpend = 1
              AND TransactionDate >= DATEADD(MONTH, -@MonthsBack, CAST(GETUTCDATE() AS date))
            GROUP BY
                DATEFROMPARTS(YEAR(TransactionDate), MONTH(TransactionDate), 1),
                CASE
                    WHEN UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%AMAZON%' THEN 'Retail'
                    WHEN UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%UBER%' OR UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%LYFT%' THEN 'Transportation'
                    WHEN UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%STARBUCKS%' OR UPPER(ISNULL(NormalizedMerchantName, '')) LIKE '%MCDONALD%' THEN 'Meals'
                    WHEN UPPER(ISNULL(Description, '')) LIKE '%SUBSCRIPTION%' OR UPPER(ISNULL(Description, '')) LIKE '%MONTHLY%' THEN 'Subscriptions'
                    ELSE 'Other'
                END
            ORDER BY MonthStart, Category;
            """;

        var results = new List<CategorySpendDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@BusinessId", SqlDbType.UniqueIdentifier) { Value = businessId });
        command.Parameters.Add(new SqlParameter("@MonthsBack", SqlDbType.Int) { Value = monthsBack });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new CategorySpendDto
            {
                MonthStart = reader.GetDateTime(reader.GetOrdinal("MonthStart")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("Amount"))
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<DuplicateChargeDto>> GetPossibleDuplicateChargesAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("businessId is required.", nameof(businessId));

        if (monthsBack <= 0)
            throw new ArgumentOutOfRangeException(nameof(monthsBack));

        const string sql = """
            SELECT
                COALESCE(
                    NULLIF(LTRIM(RTRIM(NormalizedMerchantName)), ''),
                    NULLIF(LTRIM(RTRIM(MerchantName)), ''),
                    'UNKNOWN'
                ) AS MerchantName,
                CAST(TransactionDate AS date) AS TransactionDate,
                AbsoluteAmount AS Amount,
                COUNT(*) AS DuplicateCount
            FROM dbo.Transactions
            WHERE BusinessId = @BusinessId
              AND CountsAsSpend = 1
              AND TransactionDate >= DATEADD(MONTH, -@MonthsBack, CAST(GETUTCDATE() AS date))
            GROUP BY
                COALESCE(
                    NULLIF(LTRIM(RTRIM(NormalizedMerchantName)), ''),
                    NULLIF(LTRIM(RTRIM(MerchantName)), ''),
                    'UNKNOWN'
                ),
                CAST(TransactionDate AS date),
                AbsoluteAmount
            HAVING COUNT(*) > 1
            ORDER BY COUNT(*) DESC, AbsoluteAmount DESC;
            """;

        var results = new List<DuplicateChargeDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@BusinessId", SqlDbType.UniqueIdentifier) { Value = businessId });
        command.Parameters.Add(new SqlParameter("@MonthsBack", SqlDbType.Int) { Value = monthsBack });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DuplicateChargeDto
            {
                MerchantName = reader.GetString(reader.GetOrdinal("MerchantName")),
                TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                DuplicateCount = reader.GetInt32(reader.GetOrdinal("DuplicateCount"))
            });
        }

        return results;
    }
}