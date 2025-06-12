using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;
using TeamsReportDashboard.Backend.Services.User.ResetPassword;
using TeamsReportDashboard.Backend.Services.User.Update;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Models.Dto;
using TeamsReportDashboard.Services.User.Create;
using TeamsReportDashboard.Services.User.Delete;
using TeamsReportDashboard.Services.User.Read;

namespace TeamsReportDashboard.Backend.Controllers;
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

    [HttpPut("change-my-password")] // A rota da action permanece a mesma
    public async Task<IActionResult> ChangeMyPassword(
        [FromServices] IChangeMyPasswordService service,
        [FromBody] ChangeMyPasswordDto changeMyPasswordDto) 
    {
        // Obter o ID do usuário logado a partir das claims do token JWT
        var userIdString = User.FindFirst("id")?.Value; 
        // Ou use ClaimTypes.NameIdentifier se você configurou o JWT para usar essa claim como ID
        // var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            // Isso não deveria acontecer se o usuário estiver autenticado e o token contiver a claim "id"
            return Unauthorized(new { message = "Não foi possível identificar o usuário autenticado." });
        }

        // Agora use o userId obtido do token para chamar o serviço
        await service.Execute(userId, changeMyPasswordDto);
        return NoContent();
    }
    
    
    [HttpPut("change-password")] 
    //[Authorize(Roles = "Admin, Master")] // Você precisará garantir que esta rota seja protegida para Masters
    public async Task<IActionResult> ChangePassword(
        [FromServices] IResetPasswordService service, 
        [FromBody] ResetPasswordDto resetPasswordDto,   
        int id) 
    {
        Console.WriteLine(id);
        await service.Execute(id, resetPasswordDto); // O serviço lida com a lógica
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetLoggedUser([FromServices] IGetUsersService service)
    {
        var userIdClaim = User.FindFirst("id");
        if (userIdClaim == null) return Unauthorized();

        var userId = int.Parse(userIdClaim.Value);
        var user = await service.Get(userId);

        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role
        });

    }
}