namespace FinancialIntelligence.Api.Dtos.Auth;

public class UserBusinessDto
{
    public Guid BusinessId { get; set; }
    public string BusinessKey { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string RoleName { get; set; } = "";
    public bool IsDefault { get; set; }
}