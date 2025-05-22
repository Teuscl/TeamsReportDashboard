using Microsoft.AspNetCore.Mvc;
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
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,  // se estiver usando HTTPS
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.Now.AddHours(2)
            };
            
            Response.Cookies.Append("accessToken", loginResponse.Token, cookieOptions);
            Response.Cookies.Append("refreshToken", loginResponse.RefreshToken, cookieOptions);
            
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
                // Caso o refresh token tenha expirado ou sido invalidado
                return Unauthorized(new { message = "Refresh token is invalid or expired." });
            }

            var newAccessToken = _tokenService.GenerateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7); // A expiração pode ser configurada como desejado

            _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.CommitAsync();
            
            
            // Define o NOVO Access Token como um cookie HttpOnly
            var accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, 
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.Now.AddHours(2) // Consistente com a expiração do token
            };
            Response.Cookies.Append("accessToken", newAccessToken, accessTokenCookieOptions);
            
            // Atualiza o cookie com novo refresh token
            HttpContext.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)  // Define o tempo de expiração conforme desejado
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
    
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Append("accessToken", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(-1) // Data de expiração no passado
        });
        Response.Cookies.Append("refreshToken", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(-1) // Data de expiração no passado
        });
        return Ok(new { message = "Logged out successfully" });
    }
}