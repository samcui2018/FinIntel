namespace FinancialIntelligence.Api.Dtos.Auth;

public sealed class DemoStartResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsDemo { get; set; }
}