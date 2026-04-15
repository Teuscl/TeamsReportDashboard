namespace TeamsReportDashboard.Backend.Models.PromptDto;

public record PromptVersionDetailDto(
    Guid Id,
    string Content,
    DateTime CreatedAt,
    string? CreatedByEmail
);
