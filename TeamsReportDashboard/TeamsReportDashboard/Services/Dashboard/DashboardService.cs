using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Models.Dashboard;
using TeamsReportDashboard.Interfaces;
using System.Globalization;
using System.Linq.Expressions;

namespace TeamsReportDashboard.Backend.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardDto> GetDashboardDataAsync()
    {
        var now = DateTime.UtcNow;

        // --- KPIs ---
        var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
        var totalAtendimentosMes = await _unitOfWork.ReportRepository.CountAsync(r => r.RequestDate >= firstDayOfMonth);
        var totalDepartamentos = await _unitOfWork.DepartmentRepository.CountAsync();
        var totalSolicitantes = await _unitOfWork.RequesterRepository.CountAsync();
        
        // ... (Cálculo do Tempo Médio de Resposta continua igual) ...
        string tempoMedioFormatado = "00:00:00"; // Valor padrão
        var reportsWithResponseTimeQuery = _unitOfWork.ReportRepository.GetAll().Where(r => r.FirstResponseTime > TimeSpan.Zero);
        if (await reportsWithResponseTimeQuery.AnyAsync())
        {
            var allSecondsList = await reportsWithResponseTimeQuery.Select(r => r.FirstResponseTime.TotalSeconds).ToListAsync();
            var averageSeconds = allSecondsList.Average();
            var averageTimeSpan = TimeSpan.FromSeconds(averageSeconds);
            tempoMedioFormatado = $"{(int)averageTimeSpan.TotalHours:00}:{averageTimeSpan.Minutes:00}:{averageTimeSpan.Seconds:00}";
        }


        // --- DADOS PARA GRÁFICOS ---
        
        // Gráfico 1: Atendimentos por Técnico (antigo "por Equipe")
        var atendimentosPorTecnico = await _unitOfWork.ReportRepository
            .GetAll()
            .Where(r => !string.IsNullOrEmpty(r.TechnicianName))
            .GroupBy(r => r.TechnicianName)
            .Select(g => new ChartData { Name = g.Key, Total = g.Count() })
            .OrderByDescending(cd => cd.Total)
            // .Take(5) foi removido para enviar a lista completa
            .ToListAsync();

        // Gráfico 2: Atendimentos por Departamento
        var atendimentosPorDepartamento = await _unitOfWork.ReportRepository
            .GetAll()
            .Include(r => r.Requester)
            .ThenInclude(req => req.Department)
            .Where(r => r.Requester != null && r.Requester.Department != null)
            .GroupBy(r => r.Requester.Department.Name)
            .Select(g => new ChartData { Name = g.Key, Total = g.Count() })
            .OrderByDescending(cd => cd.Total)
            // .Take(5) foi removido
            .ToListAsync();
            
        // Gráfico 3: Problemas por Categoria
        var problemasPorCategoria = await _unitOfWork.ReportRepository
            .GetAll()
            .Where(r => !string.IsNullOrEmpty(r.Category))
            .GroupBy(r => r.Category)
            .Select(g => new ChartData { Name = g.Key, Total = g.Count() })
            .OrderByDescending(cd => cd.Total)
            // .Take(5) foi removido
            .ToListAsync();

        
        // Gráfico 4: Atendimentos nos últimos 12 meses
        var twelveMonthsAgo = now.AddMonths(-11);
        var startOfPeriod = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1);
        var atendimentosMensais = await _unitOfWork.ReportRepository.GetAll().Where(r => r.RequestDate >= startOfPeriod).GroupBy(r => new { r.RequestDate.Year, r.RequestDate.Month }).Select(g => new { g.Key.Year, g.Key.Month, Total = g.Count() }).OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();
        var atendimentosPorMesFormatado = new List<ChartData>();
        for (int i = 0; i < 12; i++){ var monthDate = startOfPeriod.AddMonths(i); var monthData = atendimentosMensais.FirstOrDefault(m => m.Year == monthDate.Year && m.Month == monthDate.Month); atendimentosPorMesFormatado.Add(new ChartData { Name = monthDate.ToString("MMM/yy", new CultureInfo("pt-BR")), Total = monthData?.Total ?? 0 }); }

        // Montagem final do DTO
        var dashboardData = new DashboardDto()
        {
            TotalAtendimentosMes = totalAtendimentosMes,
            TempoMedioPrimeiraResposta = tempoMedioFormatado,
            TotalDepartamentos = totalDepartamentos,
            TotalSolicitantes = totalSolicitantes,
            ProblemasPorCategoria = problemasPorCategoria,
            AtendimentosPorTecnico = atendimentosPorTecnico, // Propriedade renomeada
            AtendimentosPorDepartamento = atendimentosPorDepartamento, // Nova propriedade
            AtendimentosPorMes = atendimentosPorMesFormatado,
        };

        return dashboardData;
    }
}