using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TeamsReportDashboard.Backend.Entities; // Para a entidade Report
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Backend.Services.Report.Create; // Para CreateReportDto e UpdateReportDto
using TeamsReportDashboard.Backend.Services.Report.Read;
using TeamsReportDashboard.Services.User.Create;
using TeamsReportDashboard.Services.User.Delete; // Supondo que suas interfaces de serviço de Report estejam aqui
// A interface IUpdateReportService já está definida em TeamsReportDashboard.Backend.Services.Report.Update.IUpdateReportService
// Vamos usar o namespace completo para ela se não houver um using geral para TeamsReportDashboard.Backend.Interfaces.Report
// ou TeamsReportDashboard.Backend.Services.Report.Update

namespace TeamsReportDashboard.Backend.Controllers
{
    [Route("[controller]")] // Será /Report
    [ApiController]
    public class ReportController : ControllerBase
    {
        // POST: /Report
        
[HttpPost]
        [Authorize(Roles = "Admin, Master")]
        public async Task<IActionResult> CreateReport(
            [FromServices] ICreateReportService service,
            [FromBody] CreateReportDto createReportDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var report = await service.Execute(createReportDto);
            return Ok();
        }
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ReportDto>>> GetAllReports( 
            [FromServices] IGetReportService service)
        {
            var reports = await service.GetAll();
            return Ok(reports);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ReportDto>> GetReportById( 
            [FromServices] IGetReportService service,
            int id)
        {
            var report = await service.Get(id);
            if (report == null)
            {
                return NotFound(new { Message = $"Relatório com ID {id} não encontrado." });
            }
            return Ok(report);
        }

        
        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin, Master")]
        public async Task<IActionResult> UpdateReport(
            [FromServices] TeamsReportDashboard.Backend.Services.Report.Update.IUpdateReportService service, // Usando o namespace completo para clareza
            int id,
            [FromBody] UpdateReportDto updateReportDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // O serviço Execute não retorna valor, mas pode lançar exceções (ex: NotFoundException)
            // que seriam tratadas pelo seu ExceptionFilter global.
            await service.Execute(id, updateReportDto);
            return NoContent(); // Padrão para atualizações bem-sucedidas sem conteúdo de retorno
        }

        
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, Master")]
        public async Task<IActionResult> DeleteReport(
            [FromServices] IDeleteReportService service,
            int id)
        {
            // O serviço Execute não retorna valor, mas pode lançar exceções (ex: NotFoundException)
            await service.Execute(id);
            return Ok(new { Message = $"Relatório com ID {id} foi deletado com sucesso." });
            // Alternativamente, poderia ser NoContent()
        }
    }
}