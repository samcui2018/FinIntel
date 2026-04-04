// public interface IUserRepository
// {
//     Task<int> GetBusinessKeyForUserAsync(
//         int userId,
//         CancellationToken cancellationToken = default);
// }
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Guid> GetBusinessKeyForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}