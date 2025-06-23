using System.Text.Json.Serialization;

namespace TeamsReportDashboard.Backend.Models.ReportDto 
{
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
        public string TempoPrimeiraResposta { get; set; } // String "HH:mm:ss"

        [JsonPropertyName("tempo_total_atendimento")]
        public string TempoTotalAtendimento { get; set; } // String "HH:mm:ss"
    }

    public class AtendimentoContainerDto
    {
        [JsonPropertyName("atendimentos")]
        public List<AtendimentoDto> Atendimentos { get; set; }
    }
}