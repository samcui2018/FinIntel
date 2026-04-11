using FinancialIntelligence.Api.Models;
namespace FinancialIntelligence.Api.Services;

public interface IGenerativeAiClient
{
    Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default);
    Task<string> ChatAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<AiChatMessage>? history = null,
        CancellationToken cancellationToken = default);
}
public sealed class AiChatMessage
{
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
}