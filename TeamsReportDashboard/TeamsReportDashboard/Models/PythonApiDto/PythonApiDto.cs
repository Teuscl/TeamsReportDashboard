using System.Text.Json.Serialization;

namespace TeamsReportDashboard.Backend.Models.PythonApiDto;

public class PythonApiDto
{
    public class PythonStartResponse
    {
        [JsonPropertyName("batch_id")] public string BatchId { get; set; } 
        
    }

    public class PythonResultResponse
    {
        [JsonPropertyName("status")] public string Status { get; set; } [JsonPropertyName("results")] public object? Results { get; set; } [JsonPropertyName("errors")] public string? Errors { get; set; }
    }
}