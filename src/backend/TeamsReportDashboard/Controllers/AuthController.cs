using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services.User.ForgotPassword;
using TeamsReportDashboard.Backend.Services.User.ResetForgottenPassword;
using TeamsReportDashboard.Backend.Services.User.ResetPassword;
using TeamsReportDashboard.Interfaces;
using LoginRequest = TeamsReportDashboard.Models.Auth.LoginRequest;

namespace TeamsReportDashboard.Backend.Controllers;
[Route("[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IUnitOfWork unitOfWork, ITokenService tokenService, IWebHostEnvironment env)
    {
        _authService = authService;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _env = env;
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = !_env.IsDevelopment(),
        SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
        Expires = expires
    };

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            var loginResponse = await _authService.LoginAsync(loginRequest);
            Response.Cookies.Append("accessToken", loginResponse.Token,
                BuildCookieOptions(DateTimeOffset.UtcNow.AddHours(2)));
            Response.Cookies.Append("refreshToken", loginResponse.RefreshToken,
                BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(7)));

            return Ok(new
            {
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
            return StatusCode(500, new { message = "An error occurred during the login process." });
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

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Unauthorized(new { message = "Refresh token is invalid or expired." });

            var newAccessToken = _tokenService.GenerateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            
            
            Response.Cookies.Append("accessToken", newAccessToken,
                BuildCookieOptions(DateTimeOffset.UtcNow.AddHours(2)));
            Response.Cookies.Append("refreshToken", newRefreshToken,
                BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(7)));

            return Ok(new { message = "Token refreshed" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while refreshing the token." });
        }
    }
    
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            var user = await _unitOfWork.UserRepository.GetByRefreshTokenAsync(refreshToken);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                _unitOfWork.UserRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        Response.Cookies.Append("accessToken", "", BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
        Response.Cookies.Append("refreshToken", "", BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromServices] IForgotPasswordService service,
        [FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        await service.Execute(forgotPasswordDto);
        return Ok(new { Message = "Se um usuário com este email existir em nosso sistema, um link para redefinição de senha foi enviado." });
    }

    [HttpPost("reset-password-forgotten")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPasswordForgotten(
        [FromServices] IResetForgottenPasswordService service,
        [FromBody] ResetForgottenPasswordDto dto )
    {
        await service.Execute(dto);
        return Ok(new { Message = "Sua senha foi redifina com sucesso" });
    }
}