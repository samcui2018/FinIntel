using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using FinancialIntelligence.Api.Configuration;
using FinancialIntelligence.Api.Dtos.Auth;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services;

public sealed class DemoSessionService : IDemoSessionService
{
    private readonly string _connectionString;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<DemoSessionService> _logger;
    private readonly IDemoDataSeeder _demoDataSeeder;

    public DemoSessionService(
        IConfiguration configuration,
        IOptions<JwtSettings> jwtOptions,
        ILogger<DemoSessionService> logger,
        IDemoDataSeeder demoDataSeeder)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Missing connection string.");

        _jwtSettings = jwtOptions.Value
            ?? throw new InvalidOperationException("Missing JWT settings.");

        _logger = logger;
        _demoDataSeeder = demoDataSeeder;
    }

    public async Task<DemoStartResponseDto> StartDemoAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var demoUserId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var businessName = $"Demo Business {utcNow:MMddHHmm}";
        var expiresAtUtc = utcNow.AddHours(2);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);
        var transaction = (SqlTransaction)dbTransaction;

        try
        {
            const string insertUserSql = """
                INSERT INTO dbo.Users
                (
                    UserId,
                    Email,
                    PasswordHash,
                    Role,
                    CreatedAt,
                    FirstName,
                    LastName,
                    IsActive
                )
                VALUES
                (
                    @UserId,
                    @Email,
                    @PasswordHash,
                    @Role,
                    @CreatedAt,
                    @FirstName,
                    @LastName,
                    @IsActive
                );
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertUserSql,
                    new
                    {
                        UserId = demoUserId,
                        Email = $"demo-{demoUserId:N}@finintel.local",
                        PasswordHash = "DEMO_SESSION_NO_PASSWORD",
                        Role = "DemoUser",
                        CreatedAt = utcNow,
                        FirstName = "Demo",
                        LastName = "User",
                        IsActive = true
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            const string insertBusinessSql = """
                INSERT INTO dbo.Businesses
                (
                    BusinessId,
                    BusinessName,
                    IsDemo,
                    ExpiresAtUtc,
                    CreatedAt
                )
                VALUES
                (
                    @BusinessId,
                    @BusinessName,
                    @IsDemo,
                    @ExpiresAtUtc,
                    @CreatedAt
                );
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertBusinessSql,
                    new
                    {
                        BusinessId = businessId,
                        BusinessName = businessName,
                        IsDemo = true,
                        ExpiresAtUtc = expiresAtUtc,
                        CreatedAt = utcNow
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            const string insertUserBusinessSql = """
                INSERT INTO dbo.UserBusinesses
                (
                    UserId,
                    BusinessId,
                    RoleName,
                    IsDefault,
                    CreatedAt,
                    UserBusinessId
                )
                VALUES
                (
                    @UserId,
                    @BusinessId,
                    @RoleName,
                    @IsDefault,
                    @CreatedAt,
                    @UserBusinessId
                );
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertUserBusinessSql,
                    new
                    {
                        UserId = demoUserId,
                        BusinessId = businessId,
                        RoleName = "Owner",
                        IsDefault = true,
                        CreatedAt = utcNow,
                        UserBusinessId = Guid.NewGuid()
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            var loadId = await _demoDataSeeder.SeedAsync(
                connection,
                transaction,
                businessId,
                demoUserId,
                cancellationToken);

            var token = BuildDemoJwt(demoUserId, businessId, expiresAtUtc);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Created demo session. DemoUserId={DemoUserId}, BusinessId={BusinessId}, LoadId={LoadId}, ExpiresAtUtc={ExpiresAtUtc}",
                demoUserId,
                businessId,
                loadId,
                expiresAtUtc);

            return new DemoStartResponseDto
            {
                Token = token,
                BusinessId = businessId,
                BusinessName = businessName,
                ExpiresAtUtc = expiresAtUtc,
                IsDemo = true
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to create demo session. DemoUserId={DemoUserId}, BusinessId={BusinessId}",
                demoUserId,
                businessId);

            throw;
        }
    }

    private string BuildDemoJwt(Guid demoUserId, Guid businessId, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(_jwtSettings.Key))
            throw new InvalidOperationException("Jwt:Key is missing.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, demoUserId.ToString()),
            new(ClaimTypes.NameIdentifier, demoUserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, $"demo-{demoUserId:N}@finintel.local"),
            new(ClaimTypes.Role, "DemoUser"),
            new("business_id", businessId.ToString()),
            new("is_demo", "true")
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}