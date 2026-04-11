namespace FinancialIntelligence.Api.Models;

public class AiChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatTurnDto>? History { get; set; }
}

public class ChatTurnDto
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}