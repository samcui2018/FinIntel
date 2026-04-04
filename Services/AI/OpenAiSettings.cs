namespace FinancialIntelligence.Api.Services.Ai;

public sealed class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    // Recommended default model
    public string Model { get; set; } = "gpt-4.1-mini";
}