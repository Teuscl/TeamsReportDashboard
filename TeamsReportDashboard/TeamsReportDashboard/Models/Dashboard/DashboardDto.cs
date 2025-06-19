namespace TeamsReportDashboard.Backend.Models.Dashboard;

public class DashboardDto
{
    // Cards de KPI
    public int TotalAtendimentosMes { get; set; }
    public string TempoMedioPrimeiraResposta { get; set; } // Manter como string por simplicidade
    public int TotalDepartamentos { get; set; }
    public int TotalSolicitantes { get; set; }

    // Dados para Gráficos
    public List<ChartData> AtendimentosPorMes { get; set; } = new();
    public List<ChartData> ProblemasPorCategoria { get; set; } = new();
    public List<ChartData> AtendimentosPorEquipe { get; set; } = new();
}


public class ChartData
{
    public string Name { get; set; } = string.Empty; // Ex: "Jan", "Hardware", "Suporte N1"
    public int Total { get; set; }
}