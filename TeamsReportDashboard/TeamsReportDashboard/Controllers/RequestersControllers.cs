using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.Requester;
using TeamsReportDashboard.Backend.Services.Requester.BulkCreate;
using TeamsReportDashboard.Backend.Services.Requester.Create;
using TeamsReportDashboard.Backend.Services.Requester.Delete;
using TeamsReportDashboard.Backend.Services.Requester.Read;
using TeamsReportDashboard.Backend.Services.Requester.Update;

namespace TeamsReportDashboard.Backend.Controllers;

[Route("[controller]")]
[ApiController]
public class RequestersController : Controller
{
    [HttpGet]
    [Authorize(Roles = "Admin, Master")]
    public async Task<ActionResult<IEnumerable<RequesterDto>>> GetAll(
        [FromServices] IGetRequestersService service)
    {
        return Ok(await service.GetAll());
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin, Master")]
    public async Task<ActionResult<RequesterDto>> GetById([FromServices] IGetRequestersService service, int id)
    {
        // Busca o requester no serviço
        var requester = await service.Get(id);

        // Verifica se o requester foi encontrado
        if (requester == null)
        {
            // Se não foi encontrado, retorna um 404 Not Found
            return NotFound(new { Message = $"Solicitante com ID {id} não encontrado." });
        }
        // Se foi encontrado, retorna 200 OK com o objeto requester no corpo da resposta
        return Ok(requester);
    }

    [HttpPost]
    [Authorize(Roles = "Admin, Master")]
    public async Task<ActionResult<CreateRequesterDto>> Create(
        [FromServices] ICreateRequesterService service,
        [FromBody] CreateRequesterDto createDto)
    {
        // O serviço irá validar o DTO e a lógica de negócio (ex: email duplicado)
        var newRequesterDto = await service.Execute(createDto);

        // Retorna 201 Created com o DTO do novo objeto e um link para ele no Header Location
        return Ok(createDto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> Update(
        [FromServices] IUpdateRequesterService service,
        int id,
        [FromBody] UpdateRequesterDto updateDto)
    {
        // O serviço irá validar o DTO e a lógica de negócio
        await service.Execute(id, updateDto);

        // Retorna 204 No Content, indicando sucesso sem retornar corpo
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> Delete(
        [FromServices] IDeleteRequesterService service,
        int id)
    {
        // O serviço irá verificar se o solicitante existe antes de deletar
        await service.Execute(id);

        // Retorna 204 No Content
        return NoContent();
    }


    [HttpPost("bulk-insert")]
    [Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> BulkInsert([FromServices] IBulkCreateRequesterService service,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "None file was uploaded!"});
        
        var result = await service.Execute(file);

        if (result.HasErrors)
        {
            return StatusCode(207, result); 
        }
        
        return StatusCode(201,result);
    }
}