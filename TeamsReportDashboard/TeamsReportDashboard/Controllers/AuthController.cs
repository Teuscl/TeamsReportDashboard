using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Auth;
using LoginRequest = TeamsReportDashboard.Models.Auth.LoginRequest;

namespace TeamsReportDashboard.Controllers;
[Route("[controller]")]
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
            HttpContext.Response.Cookies.Append("refreshToken", loginResponse.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(2) // 2 dias de validade
            });
            return Ok(new
            {
                token = loginResponse.Token,
                name = loginResponse.Name,
                role = loginResponse.Role,
                id = loginResponse.Id
            });
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
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out string refreshToken))
                return Unauthorized(new { message = "Refresh token is missing." });

            var user = await _unitOfWork.UserRepository.GetByRefreshTokenAsync(refreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return Unauthorized(new { message = "Refresh token is invalid or expired." });
            }

            var newAccessToken = _tokenService.GenerateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(2);

            _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.CommitAsync();

            // Atualiza o cookie com novo refresh token
            HttpContext.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(2)
            });

            return Ok(new
            {
                token = newAccessToken
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while refreshing the token.",
                details = ex.Message
            });
        }
    }
}