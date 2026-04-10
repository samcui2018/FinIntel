using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Repositories;

public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Guid> CreateUserAsync(string email, string passwordHash, string role = "User", CancellationToken cancellationToken = default);
}