using TeamsReportDashboard.Entities.Enums;

namespace TeamsReportDashboard.Backend.Models.UserDto;

public record UserDto(Guid Id, string Name, string Email, UserRole Role, bool IsActive);
