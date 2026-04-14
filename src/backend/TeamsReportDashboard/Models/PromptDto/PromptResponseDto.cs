namespace TeamsReportDashboard.Backend.Models.PromptDto;

public record PromptResponseDto(
    string Content,
    DateTime? LastUpdatedAt,
    string? LastUpdatedBy,
    IReadOnlyList<PromptHistoryEntryDto> History
);
