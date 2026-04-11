using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services;

public interface IAiChatService
{
    Task<AiChatResponse> ChatAsync(
        Guid userId,
        Guid businessId,
        AiChatRequest request,
        CancellationToken cancellationToken = default);
}