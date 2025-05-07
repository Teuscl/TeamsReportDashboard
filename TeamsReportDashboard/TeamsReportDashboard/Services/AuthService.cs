using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Auth;

namespace TeamsReportDashboard.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IPasswordService _passwordService;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IConfiguration configuration, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _configuration = configuration;
        _passwordService = passwordService;
        
    }
    
    public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
    {
        var user = await _unitOfWork.UserRepository.GetByEmailAsync(loginRequest.Email);
        if(user == null)
            throw new UnauthorizedAccessException("User not found");
        
        var result = _passwordService.VerifyPassword(loginRequest.Password, user.Password);
        if (!result)
        {
            throw new UnauthorizedAccessException("Invalid password");
        }
        var token = _tokenService.GenerateToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiryTime = DateTime.Now.AddDays(7);
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshTokenExpiryTime;
        
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.CommitAsync();

        return new LoginResponse()
        {
            Token = token,
            RefreshToken = refreshToken,
            Name = user.Name,
            Role = user.Role.ToString()
        };
    }
}