using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Auth;

namespace TeamsReportDashboard.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _passwordService = passwordService;
    }
    public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
    {
        var user = await _unitOfWork.UserRepository.GetByEmailAsync(loginRequest.Email);
        var isValid = user != null && user.IsActive &&
                      _passwordService.VerifyPassword(loginRequest.Password, user.Password);
        if (!isValid)
            throw new UnauthorizedAccessException("Invalid credentials");
        var token = _tokenService.GenerateToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshTokenExpiryTime;
        
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return new LoginResponse()
        {
            Id = user.Id,
            Token = token,
            RefreshToken = refreshToken,
            Name = user.Name,
            Role = user.Role.ToString()
        };
    }
}