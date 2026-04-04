using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Adapters;

public interface ISourceAdapter
{
    string SourceType { get; }

    Task<IReadOnlyList<CanonicalTransaction>> ParseAsync(
        Stream fileStream,
        string sourceName,
        Guid businessId,
        Guid loadId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);
}