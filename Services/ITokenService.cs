using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services;

public interface ITokenService
{
    string CreateToken(User user);
}