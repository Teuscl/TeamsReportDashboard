using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Models.Dto;
using TeamsReportDashboard.Services.User.ChangePassword;
using TeamsReportDashboard.Services.User.Create;
using TeamsReportDashboard.Services.User.Delete;
using TeamsReportDashboard.Services.User.Read;
using TeamsReportDashboard.Services.User.Update;

namespace TeamsReportDashboard.Controllers;
[Route("[controller]")]
[ApiController]
public class UserController : Controller
{
    [HttpPost]
    //[Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> Create(
        [FromServices]ICreateUserService service,
        [FromBody] CreateUserDto createUserDto)
    {
        var user = await service.Execute(createUserDto);
        return Ok(user);
    }

    [HttpPut]
    ///[Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> Update(
        [FromServices]IUpdateUserService service,
        [FromBody] UpdateUserDto updateUserDto)
    {
        await service.Execute(updateUserDto);
        return NoContent();
    }
    
    [HttpDelete]
    //[Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> Delete(
        [FromServices]IDeleteUserService service,
        int id)
    {
        var currentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUser == id)
        {
            return BadRequest(new { message = "You cannot delete your own account." });
        }
        await service.Execute(id);
        return Ok(new {Message = "User has been deleted"});
    }

    [HttpGet]
    //[Authorize]
    public async Task<ActionResult<IEnumerable<User>>>GetAll(
        [FromServices]IGetUsersService service)
    {
        return Ok(await service.GetAll());
    }

    [HttpPut("change-password")]
    //[Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> ChangePassword(
        [FromServices]IChangePasswordService service,
        [FromBody] ChangePasswordDto changePasswordDto, int id)
    {
        await service.Execute(id, changePasswordDto);
        return NoContent();
    }
}