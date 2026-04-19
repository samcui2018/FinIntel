using Microsoft.Data.SqlClient;

namespace FinancialIntelligence.Api.Repositories;

public static class DbRetryHelper
{
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default,
        int maxRetries = 2,
        int baseDelayMs = 200,
        SqlConnection? connectionForPoolClear = null)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (SqlException ex) when (attempt < maxRetries && IsTransient(ex))
            {
                // Optional but useful: clear bad pooled connection
                if (connectionForPoolClear != null)
                {
                    SqlConnection.ClearPool(connectionForPoolClear);
                }

                var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new Exception("Database operation failed after retries.");
    }

    private static bool IsTransient(SqlException ex)
    {
        // Basic transient detection (you can expand this later)
        return ex.Errors.Cast<SqlError>().Any(e =>
            e.Number == -2     // timeout
            || e.Number == 4060 // cannot open database
            || e.Number == 40197 // Azure transient
            || e.Number == 40501 // throttling
            || e.Number == 40613 // DB unavailable
        );
    }
}