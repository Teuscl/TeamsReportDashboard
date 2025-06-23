using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsReportDashboard.Backend.Models.Dashboard;
using TeamsReportDashboard.Backend.Services.Dashboard;

namespace TeamsReportDashboard.Backend.Controllers;


[Route("[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetData()
    {
        var data = await _service.GetDashboardDataAsync();
        return Ok(data);
    }
}