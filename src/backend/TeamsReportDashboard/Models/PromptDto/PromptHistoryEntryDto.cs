namespace TeamsReportDashboard.Backend.Models.PromptDto;

public record PromptHistoryEntryDto(
    Guid Id,
    string ContentPreview,
    DateTime CreatedAt,
    string? CreatedByEmail
);
