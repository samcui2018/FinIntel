using Microsoft.Data.SqlClient;
namespace FinancialIntelligence.Api.Repositories;

public interface IDemoDataSeeder
{
    Task<Guid> SeedAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid businessId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
}