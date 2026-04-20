using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsReportDashboard.Backend.Models.PromptDto;
using TeamsReportDashboard.Backend.Services.Prompt.Read;
using TeamsReportDashboard.Backend.Services.Prompt.Update;
using TeamsReportDashboard.Exceptions;

namespace TeamsReportDashboard.Backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Master")]
public class PromptController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PromptResponseDto>> GetPrompt(
        [FromServices] IGetPromptService service,
        CancellationToken ct)
    {
        return Ok(await service.ExecuteAsync(ct));
    }

    [HttpGet("history/{id:guid}")]
    public async Task<ActionResult<PromptVersionDetailDto>> GetPromptVersion(
        Guid id,
        [FromServices] IGetPromptVersionService service,
        CancellationToken ct)
    {
        var result = await service.ExecuteAsync(id, ct);
        if (result is null) return NotFound(new { message = "Versão não encontrada." });
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdatePrompt(
        [FromServices] IUpdatePromptService service,
        [FromBody] PromptDto dto,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Não foi possível identificar o usuário autenticado." });

        try
        {
            await service.ExecuteAsync(dto, userId, ct);
            return NoContent();
        }
        catch (ErrorOnValidationException ex)
        {
            return BadRequest(new { errors = ex.GetErrorMessages() });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }
}
