using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;
using TeamsReportDashboard.Backend.Services.User.ResetPassword;
using TeamsReportDashboard.Backend.Services.User.Update;
using TeamsReportDashboard.Models.Dto;
using TeamsReportDashboard.Services.User.Create;
using TeamsReportDashboard.Services.User.Delete;
using TeamsReportDashboard.Services.User.Read;
using TeamsReportDashboard.Backend.Services.User.UpdateMyProfile;

namespace TeamsReportDashboard.Backend.Controllers;
[Route("[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> Create(
        [FromServices]ICreateUserService service,
        [FromBody] CreateUserDto createUserDto)
    {
        var user = await service.Execute(createUserDto);
        return Ok(user);
    }

    [HttpPut]
    [Authorize(Roles = "Admin, Master")]
    
    public async Task<IActionResult> Update(
        [FromServices]IUpdateUserService service,
        [FromBody] UpdateUserDto updateUserDto)
    {
        await service.Execute(updateUserDto);
        return NoContent();
    }
    
    [HttpDelete]
    [Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> Delete(
        [FromServices]IDeleteUserService service,
        Guid id)
    {
        var currentUserStr = User.FindFirst("id")?.Value;
        if (!Guid.TryParse(currentUserStr, out var currentUser))
            return Unauthorized();
        if (currentUser == id)
        {
            return BadRequest(new { message = "You cannot delete your own account." });
        }
        await service.Execute(id);
        return Ok(new {Message = "User has been deleted"});
    }

    [HttpGet]
    [Authorize(Roles = "Admin, Master")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(
        [FromServices] IGetUsersService service)
    {
        return Ok(await service.GetAll());
    }

    [HttpPut("change-my-password")]
    [Authorize]
    public async Task<IActionResult> ChangeMyPassword(
        [FromServices] IChangeMyPasswordService service,
        [FromBody] ChangeMyPasswordDto changeMyPasswordDto) 
    {
        var userIdString = User.FindFirst("id")?.Value; 

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Não foi possível identificar o usuário autenticado." });
        }

        await service.Execute(userId, changeMyPasswordDto);
        return NoContent();
    }
    
    
    [HttpPut("change-password")] 
    [Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> ChangePassword(
        [FromServices] IResetPasswordService service, 
        [FromBody] ResetPasswordDto resetPasswordDto,   
        Guid id) 
    {
        await service.Execute(id, resetPasswordDto);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetLoggedUser([FromServices] IGetUsersService service)
    {
        var userIdClaim = User.FindFirst("id");
        if (userIdClaim == null) return Unauthorized();

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();
        var user = await service.Get(userId);
        return Ok(user);
    }

    [HttpPut("my-profile")]
    [Authorize]    
    public async Task<IActionResult> UpdateMyProfile(
        [FromServices] IUpdateMyProfileService service,
        [FromBody] UpdateMyProfileDto updateMyProfileDto) 
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Não foi possível identificar o usuário autenticado." });
        }

        await service.Execute(userId, updateMyProfileDto);
        return NoContent();
    }
}