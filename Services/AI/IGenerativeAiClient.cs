namespace FinancialIntelligence.Api.Services.Ai;

public interface IGenerativeAiClient
{
    Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}