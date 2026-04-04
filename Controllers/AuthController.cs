using FinancialIntelligence.Api.Configuration;
using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Dtos.Auth;
using FinancialIntelligence.Api.Repositories;
using FinancialIntelligence.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FinancialIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly IAuthService _authService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(
        IAuthService authService,
        IAuthRepository authRepository,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtOptions,
        IPasswordHasher passwordHasher)
    {
        _authRepository = authRepository;
        _tokenService = tokenService;
        _jwtSettings = jwtOptions.Value;
        _authService = authService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required.");

        var existing = await _authRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
            return Conflict("A user with that email already exists.");

        var user = new User
        {
            Email = request.Email.Trim(),
            Role = "User"
        };

        user.PasswordHash = _passwordHasher.Hash(user, request.Password);

        var newUserId = await _authRepository.CreateUserAsync(
            user.Email,
            user.PasswordHash,
            user.Role,
            cancellationToken);

        user.UserId = newUserId;

        var token = _tokenService.CreateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresMinutes)
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);

        if (result is null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(result);
    }
}
// public class AuthController : ControllerBase
// {
//     private readonly IAuthRepository _authRepository;
//     private readonly ITokenService _tokenService;
//     private readonly JwtSettings _jwtSettings;
//     private readonly PasswordHasher<User> _passwordHasher = new();
//     private readonly IAuthService _authService;

//     public AuthController(
//         IAuthService authService,
//         IAuthRepository authRepository,
//         ITokenService tokenService,
//         IOptions<JwtSettings> jwtOptions)
//     {
//         _authRepository = authRepository;
//         _tokenService = tokenService;
//         _jwtSettings = jwtOptions.Value;
//         _authService = authService;
//     }

//     [HttpPost("register")]
//     public async Task<ActionResult<AuthResponse>> Register(
//         [FromBody] RegisterRequest request,
//         CancellationToken cancellationToken)
//     {
//         if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
//             return BadRequest("Email and password are required.");

//         var existing = await _authRepository.GetByEmailAsync(request.Email, cancellationToken);
//         if (existing != null)
//             return Conflict("A user with that email already exists.");

//         var user = new User
//         {
//             Email = request.Email.Trim(),
//             Role = "User"
//         };

//         user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

//         var newUserId = await _authRepository.CreateUserAsync(
//             user.Email,
//             user.PasswordHash,
//             user.Role,
//             cancellationToken);

//         user.Id = newUserId;

//         var token = _tokenService.CreateToken(user);

//         return Ok(new AuthResponse
//         {
//             Token = token,
//             Email = user.Email,
//             Role = user.Role,
//             ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresMinutes)
//         });
//     }
//      [HttpPost("login")]
//     public async Task<ActionResult<LoginResponseDto>> Login(
//         [FromBody] LoginRequest request,
//         CancellationToken cancellationToken)
//     {
//         var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);

//         if (result is null)
//             return Unauthorized(new { message = "Invalid email or password." });

//         return Ok(result);
//     }
    // [HttpPost("login")]
    // public async Task<ActionResult<AuthResponse>> Login(
    //     [FromBody] LoginRequest request,
    //     CancellationToken cancellationToken)
    // {
    //     if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    //         return BadRequest("Email and password are required.");

    //     var user = await _authRepository.GetByEmailAsync(request.Email, cancellationToken);
    //     if (user == null)
    //         return Unauthorized("Invalid email or password.");

    //     var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
    //     if (result == PasswordVerificationResult.Failed)
    //         return Unauthorized("Invalid email or password.");

    //     var token = _tokenService.CreateToken(user);

    //     return Ok(new AuthResponse
    //     {
    //         Token = token,
    //         Email = user.Email,
    //         Role = user.Role,
    //         BusinessId = user.BusinessId,
    //         ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresMinutes)
    //     });
    // }
