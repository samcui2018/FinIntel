using FinancialIntelligence.Api.Models;
namespace FinancialIntelligence.Api.Services;

public interface IPasswordHasher
{
    string Hash(User user, string password);
    bool Verify(User user, string hashedPassword, string providedPassword);
}