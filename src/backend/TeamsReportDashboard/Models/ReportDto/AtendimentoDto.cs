// Local: TeamsReportDashboard.Backend/Models/ReportDto/AtendimentoDtos.cs

using System.Text.Json.Serialization;

namespace TeamsReportDashboard.Backend.Models.ReportDto 
{
    // Representa cada atendimento individual
    public class AtendimentoDto
    {
        [JsonPropertyName("quem_solicitou_atendimento")]
        public string QuemSolicitouAtendimento { get; set; }

        [JsonPropertyName("email_solicitante")]
        public string EmailSolicitante { get; set; }

        [JsonPropertyName("quem_respondeu")]
        public string QuemRespondeu { get; set; }

        [JsonPropertyName("data_solicitacao")]
        public string DataSolicitacao { get; set; }

        [JsonPropertyName("hora_primeira_mensagem")]
        public string HoraPrimeiraMensagem { get; set; }

        [JsonPropertyName("problema_relatado")]
        public string ProblemaRelatado { get; set; }

        [JsonPropertyName("categoria")]
        public string Categoria { get; set; }

        [JsonPropertyName("tempo_primeira_resposta")]
        public string TempoPrimeiraResposta { get; set; }

        // CORREÇÃO CRÍTICA: Alterado de 'string' para 'long' para receber o número do JSON
        [JsonPropertyName("tempo_total_atendimento")]
        public long TempoTotalAtendimento { get; set; }
    }

    // Representa o contêiner que tem a lista de atendimentos
    public class AtendimentoContainerDto
    {
        [JsonPropertyName("atendimentos")]
        public List<AtendimentoDto> Atendimentos { get; set; } = new();
    }
}