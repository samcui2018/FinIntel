namespace FinancialIntelligence.Api.Dtos.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public UserDto User { get; set; } = new();
    public List<UserBusinessDto> Businesses { get; set; } = [];
    public Guid? CurrentBusinessId { get; set; }
}

