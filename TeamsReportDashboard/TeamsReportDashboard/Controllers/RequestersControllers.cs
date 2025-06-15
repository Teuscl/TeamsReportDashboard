using Microsoft.AspNetCore.Mvc;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Services.Requester.Read;
using TeamsReportDashboard.Interfaces;

[Route("[controller]")]
[ApiController]
public class RequestersController : Controller
{
    [HttpGet]
    //[Authorize]
    public async Task<ActionResult<IEnumerable<Requester>>>GetAll(
        [FromServices]IGetRequestersService service)
    {
        return Ok(await service.GetAll());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Requester>> Get([FromServices] IGetRequestersService service, int id)
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
}