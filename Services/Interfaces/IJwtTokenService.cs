using FinancialIntelligence.Api.Models;
namespace FinancialIntelligence.Api.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}