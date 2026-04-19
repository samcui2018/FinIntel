using System.Data;
using Microsoft.Data.SqlClient;
using FinancialIntelligence.Api.Dtos.Businesses;

namespace FinancialIntelligence.Api.Repositories;

public sealed class BusinessRepository : IBusinessRepository
{
    private readonly string _connectionString;

    public BusinessRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Connection string 'FinIntelConnection' is missing.");
        Console.WriteLine($"Connection string exists: {!string.IsNullOrEmpty(_connectionString)}");
    }

    public async Task<IReadOnlyList<BusinessResponse>> GetBusinessesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT
            b.BusinessId,
            b.BusinessName,
            b.LegalName,
            b.Industry,
            b.Website,
            b.Phone,
            ub.RoleName,
            ub.IsDefault,
            b.IsActive
        FROM dbo.UserBusinesses ub
        INNER JOIN dbo.Businesses b
            ON b.BusinessId = ub.BusinessId
        WHERE ub.UserId = @UserId
        ORDER BY ub.IsDefault DESC, b.BusinessName ASC;
        """;
        
        var results = new List<BusinessResponse>();
        return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new BusinessResponse
                {
                    BusinessId = reader.GetGuid(reader.GetOrdinal("BusinessId")),
                    BusinessName = reader.GetString(reader.GetOrdinal("BusinessName")),
                    LegalName = reader["LegalName"] as string,
                    Industry = reader["Industry"] as string,
                    Website = reader["Website"] as string,
                    Phone = reader["Phone"] as string,
                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                    IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            return results;
        }, cancellationToken);
    }

    public async Task<BusinessDetailResponse?> GetBusinessForUserAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT
            b.BusinessId,
            b.BusinessName,
            b.LegalName,
            b.Industry,
            b.Website,
            b.Phone,
            b.TaxId,
            b.AddressLine1,
            b.AddressLine2,
            b.City,
            b.StateProvince,
            b.PostalCode,
            b.Country,
            b.CreatedAt,
            b.IsActive,
            ub.RoleName,
            ub.IsDefault
        FROM dbo.UserBusinesses ub
        INNER JOIN dbo.Businesses b
            ON b.BusinessId = ub.BusinessId
        WHERE ub.UserId = @UserId
          AND ub.BusinessId = @BusinessId;
        """;
         return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
            command.Parameters.Add("@BusinessId", SqlDbType.UniqueIdentifier).Value = businessId;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new BusinessDetailResponse
            {
                BusinessId = reader.GetGuid(reader.GetOrdinal("BusinessId")),
                BusinessName = reader.GetString(reader.GetOrdinal("BusinessName")),
                LegalName = reader["LegalName"] as string,
                Industry = reader["Industry"] as string,
                Website = reader["Website"] as string,
                Phone = reader["Phone"] as string,
                TaxId = reader["TaxId"] as string,
                AddressLine1 = reader["AddressLine1"] as string,
                AddressLine2 = reader["AddressLine2"] as string,
                City = reader["City"] as string,
                StateProvince = reader["StateProvince"] as string,
                PostalCode = reader["PostalCode"] as string,
                Country = reader["Country"] as string,
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault"))
            };
        }, cancellationToken); 
    }

    public async Task<Guid> CreateBusinessAsync(
        Guid userId,
        CreateBusinessRequest request,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        DECLARE @InsertedBusiness TABLE (BusinessId UNIQUEIDENTIFIER);

        INSERT INTO dbo.Businesses
        (
            BusinessName,
            LegalName,
            Industry,
            Website,
            Phone,
            TaxId,
            AddressLine1,
            AddressLine2,
            City,
            StateProvince,
            PostalCode,
            Country
        )
        OUTPUT INSERTED.BusinessId INTO @InsertedBusiness(BusinessId)
        VALUES
        (
            @BusinessName,
            @LegalName,
            @Industry,
            @Website,
            @Phone,
            @TaxId,
            @AddressLine1,
            @AddressLine2,
            @City,
            @StateProvince,
            @PostalCode,
            @Country
        );

        DECLARE @BusinessId UNIQUEIDENTIFIER;
        SELECT TOP 1 @BusinessId = BusinessId FROM @InsertedBusiness;

        DECLARE @HasDefault BIT = 0;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.UserBusinesses
            WHERE UserId = @UserId
              AND IsDefault = 1
        )
        BEGIN
            SET @HasDefault = 1;
        END

        INSERT INTO dbo.UserBusinesses
        (
            UserBusinessId,
            UserId,
            BusinessId,
            RoleName,
            IsDefault,
            CreatedAt
        )
        VALUES
        (
            NEWID(),
            @UserId,
            @BusinessId,
            'Owner',
            CASE WHEN @HasDefault = 1 THEN 0 ELSE 1 END,
            SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;

        SELECT @BusinessId;
        """;
         return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
            command.Parameters.Add("@BusinessName", SqlDbType.NVarChar, 200).Value = request.BusinessName.Trim();
            command.Parameters.Add("@LegalName", SqlDbType.NVarChar, 200).Value = (object?)request.LegalName ?? DBNull.Value;
            command.Parameters.Add("@Industry", SqlDbType.NVarChar, 100).Value = (object?)request.Industry ?? DBNull.Value;
            command.Parameters.Add("@Website", SqlDbType.NVarChar, 300).Value = (object?)request.Website ?? DBNull.Value;
            command.Parameters.Add("@Phone", SqlDbType.NVarChar, 50).Value = (object?)request.Phone ?? DBNull.Value;
            command.Parameters.Add("@TaxId", SqlDbType.NVarChar, 50).Value = (object?)request.TaxId ?? DBNull.Value;
            command.Parameters.Add("@AddressLine1", SqlDbType.NVarChar, 200).Value = (object?)request.AddressLine1 ?? DBNull.Value;
            command.Parameters.Add("@AddressLine2", SqlDbType.NVarChar, 200).Value = (object?)request.AddressLine2 ?? DBNull.Value;
            command.Parameters.Add("@City", SqlDbType.NVarChar, 100).Value = (object?)request.City ?? DBNull.Value;
            command.Parameters.Add("@StateProvince", SqlDbType.NVarChar, 100).Value = (object?)request.StateProvince ?? DBNull.Value;
            command.Parameters.Add("@PostalCode", SqlDbType.NVarChar, 30).Value = (object?)request.PostalCode ?? DBNull.Value;
            command.Parameters.Add("@Country", SqlDbType.NVarChar, 100).Value = (object?)request.Country ?? DBNull.Value;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return (Guid)result!;
        }, cancellationToken);
    }

    public async Task<bool> UpdateBusinessAsync(
        Guid userId,
        Guid businessId,
        UpdateBusinessRequest request,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        UPDATE b
        SET
            b.BusinessName = @BusinessName,
            b.LegalName = @LegalName,
            b.Industry = @Industry,
            b.Website = @Website,
            b.Phone = @Phone,
            b.TaxId = @TaxId,
            b.AddressLine1 = @AddressLine1,
            b.AddressLine2 = @AddressLine2,
            b.City = @City,
            b.StateProvince = @StateProvince,
            b.PostalCode = @PostalCode,
            b.Country = @Country,
            b.IsActive = @IsActive
        FROM dbo.Businesses b
        INNER JOIN dbo.UserBusinesses ub
            ON ub.BusinessId = b.BusinessId
        WHERE ub.UserId = @UserId
          AND b.BusinessId = @BusinessId;
        """;
         return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
            command.Parameters.Add("@BusinessId", SqlDbType.UniqueIdentifier).Value = businessId;
            command.Parameters.Add("@BusinessName", SqlDbType.NVarChar, 200).Value = request.BusinessName.Trim();
            command.Parameters.Add("@LegalName", SqlDbType.NVarChar, 200).Value = (object?)request.LegalName ?? DBNull.Value;
            command.Parameters.Add("@Industry", SqlDbType.NVarChar, 100).Value = (object?)request.Industry ?? DBNull.Value;
            command.Parameters.Add("@Website", SqlDbType.NVarChar, 300).Value = (object?)request.Website ?? DBNull.Value;
            command.Parameters.Add("@Phone", SqlDbType.NVarChar, 50).Value = (object?)request.Phone ?? DBNull.Value;
            command.Parameters.Add("@TaxId", SqlDbType.NVarChar, 50).Value = (object?)request.TaxId ?? DBNull.Value;
            command.Parameters.Add("@AddressLine1", SqlDbType.NVarChar, 200).Value = (object?)request.AddressLine1 ?? DBNull.Value;
            command.Parameters.Add("@AddressLine2", SqlDbType.NVarChar, 200).Value = (object?)request.AddressLine2 ?? DBNull.Value;
            command.Parameters.Add("@City", SqlDbType.NVarChar, 100).Value = (object?)request.City ?? DBNull.Value;
            command.Parameters.Add("@StateProvince", SqlDbType.NVarChar, 100).Value = (object?)request.StateProvince ?? DBNull.Value;
            command.Parameters.Add("@PostalCode", SqlDbType.NVarChar, 30).Value = (object?)request.PostalCode ?? DBNull.Value;
            command.Parameters.Add("@Country", SqlDbType.NVarChar, 100).Value = (object?)request.Country ?? DBNull.Value;
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = request.IsActive;

            var rows = await command.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }, cancellationToken);
    }

    public async Task<bool> DeleteBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        DELETE ub
        FROM dbo.UserBusinesses ub
        WHERE ub.UserId = @UserId
          AND ub.BusinessId = @BusinessId;

        DELETE b
        FROM dbo.Businesses b
        WHERE b.BusinessId = @BusinessId
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.UserBusinesses ub
              WHERE ub.BusinessId = b.BusinessId
          );
        """;
        return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
            command.Parameters.Add("@BusinessId", SqlDbType.UniqueIdentifier).Value = businessId;

            var rows = await command.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }, cancellationToken);
    }

    public async Task<bool> SetDefaultBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.UserBusinesses
            WHERE UserId = @UserId
              AND BusinessId = @BusinessId
        )
        BEGIN
            SELECT 0;
            RETURN;
        END

        UPDATE dbo.UserBusinesses
        SET IsDefault = 0
        WHERE UserId = @UserId
          AND IsDefault = 1;

        UPDATE dbo.UserBusinesses
        SET IsDefault = 1
        WHERE UserId = @UserId
          AND BusinessId = @BusinessId;

        SELECT 1;
        """;
        return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
            command.Parameters.Add("@BusinessId", SqlDbType.UniqueIdentifier).Value = businessId;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) == 1;
        }, cancellationToken);
    }

    public async Task<bool> UserHasAccessToBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT COUNT(1)
        FROM dbo.UserBusinesses
        WHERE UserId = @UserId
          AND BusinessId = @BusinessId;
        """;
        return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
            command.Parameters.Add("@BusinessId", SqlDbType.UniqueIdentifier).Value = businessId;

            var count = (int)await command.ExecuteScalarAsync(cancellationToken);
            return count > 0;
        }, cancellationToken);
    }
}