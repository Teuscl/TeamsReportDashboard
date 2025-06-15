using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.DepartmentDto;
using TeamsReportDashboard.Backend.Services.Department.Create;
using TeamsReportDashboard.Backend.Services.Department.Delete;
using TeamsReportDashboard.Backend.Services.Department.Read;
using TeamsReportDashboard.Backend.Services.Department.Update;

// Não se esqueça de adicionar!

namespace TeamsReportDashboard.Backend.Controllers;

[Route("[controller]")]
[ApiController]
//[Authorize(Roles = "Admin,Master")] // Protege todos os endpoints deste controller
public class DepartmentsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetDepartments(
        [FromServices] IGetDepartmentsService service)
    {
        var departments = service.GetDepartmentsAsync();
        return Ok(await departments);
    }

    
    [HttpGet("{id}")]
    public async Task<ActionResult<Department>> GetDepartment(
        [FromServices] IGetDepartmentsService service,
        int id)
    {
        var department = service.Get(id);
        return Ok(await department);
    }

    [HttpPost]
    public async Task<ActionResult> CreateDepartmentAsync(
        [FromServices] ICreateDepartmentService service,
        [FromBody] CreateDepartmentDto department)
    {
        await service.Execute(department);
        return Ok();
    }
    
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDepartmentAsync(
        [FromServices] IUpdateDepartmentService service,
        int id,
        [FromBody] UpdateDepartmentDto departmentDto)
    {
        await service.Execute(id, departmentDto);
        return NoContent(); // 204 No Content é a resposta padrão para um update bem-sucedido
    }

    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDepartmentAsync(
        [FromServices] IDeleteDepartmentService service,
        int id)
    {
        await service.Execute(id);
        return NoContent(); // 204 No Content é a resposta padrão para um delete bem-sucedido
    }
    
    
}