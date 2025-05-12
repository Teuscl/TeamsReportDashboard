using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Auth;
using LoginRequest = TeamsReportDashboard.Models.Auth.LoginRequest;

namespace TeamsReportDashboard.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public AuthController(IAuthService authService, IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _authService = authService;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            var loginResponse = await _authService.LoginAsync(loginRequest);
            return Ok(loginResponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Caso ocorra algum outro erro
            return StatusCode(500, new { message = "An error occurred during the login process.", details = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest req)
    {
        try
        {
            var user = await _unitOfWork.UserRepository.GetByRefreshTokenAsync(req.RefreshToken);
            
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Refresh token is invalid or expired." });
            }
            
            var newToken = _tokenService.GenerateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(2);
            
            _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.CommitAsync();
            
            return Ok(new LoginResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                Name = user.Name,
                Role = user.Role.ToString()
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while refreshing the token.",
                details = e.Message // ou apenas `message` se quiser esconder detalhes técnicos
            });
        }
    }
}