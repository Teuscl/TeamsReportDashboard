using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Este using pode ser removido
using TeamsReportDashboard.Backend.Interfaces;
using TeamsReportDashboard.Backend.Models.Job;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Query;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Start;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Update;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Delete;
using TeamsReportDashboard.Backend.Services.JobSynchronization;

namespace TeamsReportDashboard.Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly IStartAnalysisService _startService;
        private readonly IAnalysisJobQueryService _queryService;
        private readonly IJobManagementService _jobManagementService; // Nome da variável corrigido
        private readonly IDeleteJobService _deleteService; 
        private readonly ILogger<AnalysisController> _logger;
        private readonly IUpdateAnalysisService _updateService;

        public AnalysisController(
            IStartAnalysisService startService,
            IAnalysisJobQueryService queryService,
            IJobManagementService jobManagementService, 
            IUpdateAnalysisService updateService,
            IDeleteJobService deleteService, 
            ILogger<AnalysisController> logger)
        {
            _startService = startService;
            _queryService = queryService;
            _jobManagementService = jobManagementService;
            _updateService = updateService;
            _deleteService = deleteService;
            _logger = logger;
        }

        [HttpPost("start")]
        [Authorize(Roles = "Admin, Master")]
        [RequestSizeLimit(200 * 1024 * 1024)]
        public async Task<IActionResult> StartAnalysis([FromForm] StartJobAnalysisDto dto)
        {
            // Extrai o UserId do claim JWT
            var userIdClaim = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Não foi possível identificar o usuário autenticado." });

            try
            {
                var newJobId = await _startService.ExecuteAsync(dto, userId);
                return AcceptedAtAction(nameof(GetJobStatus), new { jobId = newJobId }, new { JobId = newJobId });
            }
            catch (ErrorOnValidationException ex)
            {
                return BadRequest(new { errors = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { message = "Serviço de análise indisponível.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao iniciar o job.");
                return StatusCode(500, new { message = "Ocorreu um erro inesperado ao iniciar o job." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin, Master")]
        public async Task<IActionResult> GetAllJobs()
        {
            var jobs = await _queryService.GetAllAsync();
            return Ok(jobs);
        }

        [HttpGet("{jobId:guid}", Name = "GetJobStatus")]
        [Authorize]
        public async Task<IActionResult> GetJobStatus(Guid jobId)
        {
            var job = await _queryService.GetByIdAsync(jobId);
            return job != null ? Ok(job) : NotFound();
        }
        
        [HttpPut("{jobId:guid}")]
        [Authorize(Roles = "Admin, Master")]
        public async Task<IActionResult> UpdateJob(Guid jobId, [FromBody] UpdateAnalysisJobDto dto)
        {
            try
            {
                await _updateService.ExecuteAsync(jobId, dto);
                return NoContent();
            }
            catch (ErrorOnValidationException ex)
            {
                return BadRequest(new { errors = ex.Message }); // CORRIGIDO
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar o job {JobId}", jobId);
                return StatusCode(500, new { message = "Ocorreu um erro inesperado ao atualizar o job." });
            }
        }

        // Endpoint de Delete ATIVADO
        [HttpDelete("{jobId:guid}")]
        [Authorize(Roles = "Admin, Master")]
        public async Task<IActionResult> DeleteJob(Guid jobId)
        {
            try
            {
                await _deleteService.Execute(jobId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao deletar o job {JobId}", jobId);
                return StatusCode(500, new { message = $"Ocorreu um erro inesperado ao deletar o job. {ex.Message}" });
            }
        }

        [HttpPost("reprocess/{jobId:guid}")]
        [Authorize(Roles = "Admin, Master")]
        public async Task<IActionResult> ReprocessJob(Guid jobId)
        {
            try
            {
                var result = await _jobManagementService.ReprocessJobAsync(jobId); // Nome da variável corrigido
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Este job foi modificado por outra operação. Por favor, atualize e tente novamente." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { message = "Serviço de análise indisponível.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no reprocessamento do job {JobId}", jobId);
                return StatusCode(500, new { message = "Ocorreu um erro inesperado no reprocessamento." });
            }
        }
    }
}