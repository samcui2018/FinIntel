namespace FinancialIntelligence.Api.Models;

public class AiChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public List<string> SuggestedFollowUps { get; set; } = new();
}