using FinancialIntelligence.Api.Dtos.Auth;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IBusinessAccessRepository _businessAccessRepository;
    // private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenService _jwtTokenService;

    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IBusinessAccessRepository businessAccessRepository,
        ITokenService jwtTokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _businessAccessRepository = businessAccessRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponseDto?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
            return null;

        var passwordValid = _passwordHasher.Verify(user, user.PasswordHash, password);
        if (!passwordValid)
            return null;

        var businesses = await _businessAccessRepository.GetBusinessesForUserAsync(user.UserId, cancellationToken);
        var currentBusiness = businesses.FirstOrDefault(x => x.IsDefault) ?? businesses.FirstOrDefault();

        var token = _jwtTokenService.CreateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            User = new UserDto
            {
                UserId = user.UserId,
                Email = user.Email
            },
            Businesses = businesses,
            CurrentBusinessId = currentBusiness?.BusinessId
        };
    }
}