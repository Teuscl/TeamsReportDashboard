using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsReportDashboard.Backend.Models.DepartmentDto;
using TeamsReportDashboard.Backend.Services.Department.Create;
using TeamsReportDashboard.Backend.Services.Department.Delete;
using TeamsReportDashboard.Backend.Services.Department.Read;
using TeamsReportDashboard.Backend.Services.Department.Update;

namespace TeamsReportDashboard.Backend.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Master")]
public class DepartmentsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentResponseDto>>> GetDepartments(
        [FromServices] IGetDepartmentsService service)
    {
        return Ok(await service.GetDepartmentsAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentResponseDto>> GetDepartment(
        [FromServices] IGetDepartmentsService service,
        Guid id)
    {
        return Ok(await service.Get(id));
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
        Guid id,
        [FromBody] UpdateDepartmentDto departmentDto)
    {
        await service.Execute(id, departmentDto);
        return NoContent(); // 204 No Content é a resposta padrão para um update bem-sucedido
    }

    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDepartmentAsync(
        [FromServices] IDeleteDepartmentService service,
        Guid id)
    {
        await service.Execute(id);
        return NoContent(); // 204 No Content é a resposta padrão para um delete bem-sucedido
    }
    
    
}