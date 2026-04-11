using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services;

public interface ISourceAdapter
{
    string Name { get; }

    bool CanHandle(FileIngestionContext context);

    Task<IReadOnlyList<CanonicalTransactionRow>> ExtractAsync(
        FileIngestionContext context,
        CancellationToken cancellationToken = default);
}