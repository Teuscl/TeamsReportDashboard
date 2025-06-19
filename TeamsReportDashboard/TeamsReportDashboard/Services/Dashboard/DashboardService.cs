using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Models.Dashboard;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Dashboard;

public class DashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardDto> GetDashboardDataAsync()
    {
        var now = DateTime.Now;
        var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // Cálculos dos KPIs (Exemplos)
        var totalAtendimentos = await _unitOfWork.ReportRepository.CountAsync(r => r.RequestDate >= firstDayOfMonth && r.RequestDate <= lastDayOfMonth);
        var totalDepartamentos = await _unitOfWork.DepartmentRepository.CountAsync();
        var totalSolicitantes = await _unitOfWork.RequesterRepository.CountAsync();

        // Cálculos para Gráficos (Exemplos)
        var problemasPorCategoria = await _unitOfWork.ReportRepository
            .GetAll() // Supondo que GetAll retorne IQueryable<Report>
            .GroupBy(r => r.Category)
            .Select(g => new ChartData { Name = g.Key, Total = g.Count() })
            .ToListAsync();

        // Você adicionaria as outras lógicas de agregação aqui (por mês, por equipe, etc.)

        var dashboardData = new DashboardDto
        {
            TotalAtendimentosMes = totalAtendimentos,
            TempoMedioPrimeiraResposta = "00:10:30", // Este cálculo pode ser complexo, mantendo fixo por enquanto
            TotalDepartamentos = totalDepartamentos,
            TotalSolicitantes = totalSolicitantes,
            ProblemasPorCategoria = problemasPorCategoria,
            // Preencha os outros dados de gráfico aqui
            AtendimentosPorMes = new List<ChartData>(), // Preencher com lógica real
            AtendimentosPorEquipe = new List<ChartData>(), // Preencher com lógica real
        };

        return dashboardData;
    }
}