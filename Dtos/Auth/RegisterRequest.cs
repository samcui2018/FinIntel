namespace FinancialIntelligence.Api.Dtos.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
// public class LoginRequest
// {
//     public string Email { get; set; } = "";
//     public string Password { get; set; } = "";
// }
public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string BusinessId { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
}