namespace FinancialIntelligence.Api.Services;

public interface IGenerativeAiClient
{
    Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}